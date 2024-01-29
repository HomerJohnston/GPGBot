using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;
using GPGBot.ChatClients;
using GPGBot.CommandHandlers.P4;
using GPGBot.ContinuousIntegration;
using GPGBot.VersionControlSystems;
using Perforce.P4;
using WatsonWebserver;
using WatsonWebserver.Core;
using WatsonWebserver.Extensions.HostBuilderExtension;

namespace GPGBot
{
	internal class Bot
	{
		IVersionControlSystem versionControlSystem;
		
		IContinuousIntegrationSystem continuousIntegrationSystem;
		
		IChatClient chatClient;

		WebserverBase webserver;
		string? webserverKey;

		Dictionary<ulong, object?> activeEmbeds = new();

		List<Config.ActionSpec> actionSpecs = new();

		readonly Dictionary<int, BuildRecord> buildRecords = new();

		public bool bShutdownRequested = false;

		public TaskCompletionSource<bool> runComplete = new();

		// ---------------------------------------------------------------
		public Bot(IVersionControlSystem inVCS, IContinuousIntegrationSystem inCI, IChatClient inChatClient, Config.Webserver webserverConfig, Config.Actions actions)
		{
			versionControlSystem = inVCS;
			continuousIntegrationSystem = inCI;
			chatClient = inChatClient;

			webserver = BuildWebServer(webserverConfig);
			webserverKey = webserverConfig.Key ?? string.Empty;

			if (versionControlSystem == null) throw new Exception("Invalid VersionControlSystem! Check config?");
			if (continuousIntegrationSystem == null) throw new Exception("Invalid ContinuousIntegrationSystem! Check config?");
			if (chatClient == null) throw new Exception("Invalid ChatClient! Check config?");
			if (webserver == null) throw new Exception("Invalid Webserver! Check config?");

			if (actions.Spec != null)
			{
				actionSpecs = actions.Spec;
			}
			else
			{
				Console.WriteLine("Warning: no action specs found!");
			}
		}

		public async Task Run()
		{
			await chatClient.Start();
			webserver.Start();
		}

		public void Stop()
		{
			chatClient.Stop();
			webserver.Stop();
		}

		Webserver BuildWebServer(Config.Webserver serverConfig)
		{
			string address = "*";

			if (serverConfig.Address == null)
			{
				Console.WriteLine($"Webserver address is not configured. Defaulting to listen on any address.");
			}
			else
			{
				address = serverConfig.Address;
			}

			int port = 1199;

			if (serverConfig.Port == null)
			{
				Console.WriteLine($"Webserver port is not configured. Defaulting to {port}.");
			}
			else
			{
				port = (int)serverConfig.Port;
			}

			HostBuilder hostBuilder = new HostBuilder(address, port, false, DefaultRoute)
				.MapAuthenticationRoute(AuthenticateRequest)
				.MapParameteRoute(HttpMethod.POST, "/on-commit", OnCommit, true) // Handles triggers from source control system
				.MapParameteRoute(HttpMethod.POST, "/build-status-update", OnBuildStatusUpdate, true) // Handles status updates from continuous integration
				.MapParameteRoute(HttpMethod.POST, "/test", Test, true)
				.MapStaticRoute(HttpMethod.GET, "/shutdown", Shutdown, true);

			return hostBuilder.Build();
		}

		// --------------------------------------
		#region WebAuth
		async Task AuthenticateRequest(HttpContextBase context)
		{
			if (webserverKey == null)
			{
				await Accept(context);
				return;
			}

			string? key = context.Request.Headers["key"];
			if (key != null && key == webserverKey)
			{
				await Accept(context);
				return;
			}

			await Reject(context);
		}

		static async Task Accept(HttpContextBase context)
		{
			await Task.CompletedTask;
		}

		static async Task Reject(HttpContextBase context)
		{
			string? incomingKey = context.Request.Headers["key"];

			Console.WriteLine("Rejected incoming request" + ((incomingKey != null) ? (", invalid key: " + (incomingKey)) : ", no key"));
			
			context.Response.StatusCode = 401;
			await context.Response.Send("Request denied.");
		}
		#endregion

		// Master goals:
		// - We want to post commits to the project's main development stream
		// - We want to launch build processes if the commit contains code files
		// - We want to ignore commits that are made by CI processes

		// Perforce process:
		// Is the committed stream of interest to us? (e.g. Megacity-Mainline)
		// If yes, is the command type of interest to us? (e.g. change-commit)

		// --------------------------------------
		async Task OnCommit(HttpContextBase context)
		{
			if (versionControlSystem == null)
			{
				context.Response.StatusCode = 500;
				await context.Response.Send(string.Format("OnCommit failed, no version control system configured!"));
				throw new Exception("No version control system, cannot proceed!");
			}

			var queryParams = GetQueryParams(context);

			string change = queryParams["change"] ?? string.Empty;
			string client = queryParams["client"] ?? string.Empty;
			string root = queryParams["root"] ?? string.Empty;
			string user = queryParams["user"] ?? string.Empty;
			string address = queryParams["address"] ?? string.Empty;
			string branch = queryParams["branch"] ?? string.Empty; // branch is used for potential git compatibility only; p4 triggers %stream% is bugged and cannot send stream name
			string type = queryParams["type"] ?? string.Empty;

			// workaround for p4 trigger bug no stream name capability
			if (branch == string.Empty)
			{
				branch = versionControlSystem.GetStream(change, client) ?? string.Empty;
			}

			Console.WriteLine($"OnCommit: change {NoneOr(change)}, client {NoneOr(client)}, root {NoneOr(root)}, user {NoneOr(user)}, address {NoneOr(address)}, branch {NoneOr(branch)}, type {NoneOr(type)}");

			List<Config.ActionSpec> matchedSpecs = actionSpecs.FindAll(spec => spec.Stream == branch || spec.Branch == branch);

			if (matchedSpecs.Count == 0)
			{
				context.Response.StatusCode = 204;
				await context.Response.Send(string.Format($"No matching action specs found! Ignoring this commit."));
				Console.WriteLine("No matching action specs found! Ignoring this commit.");
				return;
			}

			// If multiple build specs call for the commit to be posted, we only want to post it once
			HashSet<string> commitPostedTo = new();

			foreach (Config.ActionSpec spec in matchedSpecs)
			{
				if (spec.CommitWebhook != null && !commitPostedTo.Contains(spec.CommitWebhook))
				{
					string? commitWebhook = (spec.CommitWebhook == "default") ? null : spec.CommitWebhook;
					await PostCommit(change, user, branch, client, commitWebhook);
					commitPostedTo.Add(spec.CommitWebhook);
				}

				switch (type)
				{
					case "code":
					{
						string? build = spec.BuildConfigName;

						if (build != null)
						{
							bool result = await continuousIntegrationSystem.StartBuild(build);
							Console.WriteLine($"Build {build} start result: {result}");
						}
						else
						{
							Console.WriteLine("Commit type was code, but no build config was specified - doing nothing!");
						}
						break;
					}
					case "content":
					{
						break;
					}
					default:
					{
						break;
					}
				}
			}

			context.Response.StatusCode = 200;
			await context.Response.Send(string.Format("OnCommit: change {0}, client {1}, root {2}, user {3}, address {4}, stream {5}, type {6}", change, client, root, user, address, branch, type));
		}

		private string NoneOr(string s)
		{
			if (s == string.Empty) return "NONE";

			return s;
		}

		private async Task<ulong?> PostCommit(string change, string user, string branch, string client, string? commitWebhook = null)
		{
			CommitEmbedData commitEmbedData = new CommitEmbedData();
			commitEmbedData.change = change;
			commitEmbedData.user = user;
			commitEmbedData.branch = branch;
			commitEmbedData.client = client;

			commitEmbedData.description = versionControlSystem.GetCommitDescription(change) ?? "<No description>";
			return await chatClient.PostCommitMessage(commitEmbedData, commitWebhook);
		}

		// --------------------------------------
		async Task OnBuildStatusUpdate(HttpContextBase context)
		{
			if (chatClient == null)
			{
				throw new Exception("Chat client was null!");
			}

			Console.WriteLine("OnBuildStatusUpdate(" + context.Request.Query.Querystring + ")");

			var queryParams = GetQueryParams(context);

			BuildStatusEmbedData data = new BuildStatusEmbedData();

			if (!Enum.TryParse<EBuildStatus>(queryParams["buildstat"], true, out data.buildStatus))
			{
				throw new Exception("Failed to parse a valid build status!");
			}

			data.text = "TestText";
			data.buildConfig = "TestBuildConfig";

			ulong? msgID = await chatClient.PostBuildStatusEmbed(data);

			if (msgID == null)
			{
				await context.Response.Send("Remote failed to run OnBuildStatusUpdate");
			}
            else
            {
				activeEmbeds.Add((ulong)msgID, null);
				await context.Response.Send("Remote ran OnBuildStatusUpdate");
			}
		}

		// --------------------------------------
		public async Task Test(HttpContextBase context)
		{
			Console.WriteLine("Test(" + context.ToString() + ")");

			if (chatClient == null)
			{
				return;
			}

			foreach (var embed in activeEmbeds)
			{
				await chatClient.DeleteMessage(embed.Key);
			}

			await context.Response.Send("Remote ran Test");
		}

		public async Task Shutdown(HttpContextBase context)
		{
			await context.Response.Send("Bot: shutting down!");

			runComplete.SetResult(true);
		}

		// --------------------------------------
		async Task DefaultRoute(HttpContextBase context) =>
		  await context.Response.Send("Pong");

		// --------------------------------------
		static System.Collections.Specialized.NameValueCollection GetQueryParams(HttpContextBase context)
		{
			Uri urlAsURI = new(context.Request.Url.Full);
			System.Collections.Specialized.NameValueCollection queryParams = HttpUtility.ParseQueryString(context.Request.DataAsString);// urlAsURI.Query);
			
			return queryParams;
		}
	}
}
