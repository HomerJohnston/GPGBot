using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;
using Discord;
using GPGBot.ChatClients;
using GPGBot.CommandHandlers.P4;
using GPGBot.ContinuousIntegration;
using GPGBot.VersionControlSystems;
using GPGBot.Config;
using Perforce.P4;
using WatsonWebserver;
using WatsonWebserver.Core;
using WatsonWebserver.Extensions.HostBuilderExtension;
using System.Linq;

namespace GPGBot
{
	public class Bot
	{
		// --------------------------------------
		public bool bShutdownRequested = false;

		public TaskCompletionSource<bool> runComplete = new();

		IVersionControlSystem versionControlSystem;
		
		IContinuousIntegrationSystem continuousIntegrationSystem;
		
		IChatClient chatClient;

		WebserverBase webserver;
		string? webserverKey;

		// --------------------------------------
		List<Config.BuildJob> buildJobs;
		List<Config.CommitResponse> commitResponses;
		List<string> commitIgnorePhrases = new();
		List<Config.Webhook> webhooks;

		readonly Dictionary<BuildRecord, ulong> RunningBuildMessages = new();

		// ---------------------------------------------------------------
		public Bot(IVersionControlSystem inVCS, IContinuousIntegrationSystem inCI, IChatClient inChatClient, Config.BotConfig config)
		{
			// Set up major components
			versionControlSystem = inVCS;
			continuousIntegrationSystem = inCI;
			chatClient = inChatClient;

			// Set up other config data
			commitResponses = config.commitResponses.Responses ?? new();

			foreach (var x in config.commitResponses.Ignore ?? new())
			{
				if (x.Phrase != null)
				{
					commitIgnorePhrases.Add(x.Phrase);
				}
			}

			buildJobs = config.buildJobs.Job ?? new();
			webhooks = config.namedWebhooks.Webhook ?? new();

			// Set up HTTP server
			Config.WebserverConfig webserverConfig = config.webserver;

			webserver = BuildWebServer(webserverConfig);
			webserverKey = webserverConfig.Key ?? string.Empty;

			// Check for errors
			if (versionControlSystem == null) throw new Exception("Invalid VersionControlSystem! Check config?");
			if (continuousIntegrationSystem == null) throw new Exception("Invalid ContinuousIntegrationSystem! Check config?");
			if (chatClient == null) throw new Exception("Invalid ChatClient! Check config?");
			if (webserver == null) throw new Exception("Invalid Webserver! Check config?");

			if (commitResponses.Count == 0) { LogSync("Error: Found no commit responses!"); }
			if (buildJobs.Count == 0) { LogSync("Error: Found no build jobs!"); }
			if (webhooks.Count == 0) { LogSync("Warning: Found no named webhooks."); }
		}

		// ---------------------------------------------------------------
		public async Task Start()
		{
			await chatClient.Start();
			webserver.Start();
		}

		// ---------------------------------------------------------------
		public void Stop()
		{
			chatClient.Stop();
			webserver.Stop();
		}

		// ---------------------------------------------------------------
		Webserver BuildWebServer(Config.WebserverConfig serverConfig)
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
			string buildParam = queryParams["build"] ?? string.Empty;
			
			// workaround for p4 trigger bug - no sending of stream name capability. query for it instead.
			if (branch == string.Empty)
			{
				branch = versionControlSystem.GetStream(change, client) ?? string.Empty;
			}

			if (change == string.Empty || client == string.Empty || root == string.Empty || user == string.Empty || address == string.Empty || branch == string.Empty)
			{
				Console.WriteLine($"OnCommit INVALID - something wasn't set: change {NoneOr(change)}, client {NoneOr(client)}, root {NoneOr(root)}, user {NoneOr(user)}, address {NoneOr(address)}, branch {NoneOr(branch)}, build {NoneOr(buildParam)}");
				return;
			}

			await HandleValidCommit(context, change, client, root, user, address, branch, buildParam);
		}

		async Task HandleValidCommit(HttpContextBase context, string change, string client, string root, string user, string address, string branch, string buildParam)
		{
			List<Config.CommitResponse> matchedCommits = commitResponses.FindAll(spec => spec.Name == branch);

			Console.WriteLine($"OnCommit: change {NoneOr(change)}, client {NoneOr(client)}, root {NoneOr(root)}, user {NoneOr(user)}, address {NoneOr(address)}, branch {NoneOr(branch)}, build {NoneOr(buildParam)}");

			if (matchedCommits.Count == 0)
			{
				context.Response.StatusCode = 204;
				await context.Response.Send(string.Format($"No matching action specs found! Ignoring this commit."));
				Console.WriteLine("No matching action specs found! Ignoring this commit.");
				return;
			}

			bool build;
			bool.TryParse(buildParam, out build);

			// Simple work to avoid posting the same commit twice
			HashSet<string> commitPostedTo = new();

			foreach (Config.CommitResponse spec in matchedCommits)
			{
				
				if (spec.CommitWebhook != null && !commitPostedTo.Contains(spec.CommitWebhook))
				{
					string? commitWebhook = spec.CommitWebhook;

					if (commitWebhook != null)
					{
						await PostCommit(change, user, branch, client, commitWebhook);
						commitPostedTo.Add(spec.CommitWebhook);
					}
				}

				if (build)
				{
					string? jobName = spec.BuildJob;

					if (jobName != null)
					{
						bool result = await continuousIntegrationSystem.StartJob(jobName);
						Console.WriteLine($"Build {jobName} start result: {result}");
					}
					else
					{
						Console.WriteLine("Commit contained build request flag, but no build config was specified - doing nothing!");
					}
				}
			}

			context.Response.StatusCode = 200;
			await context.Response.Send(string.Format("OnCommit: change {0}, client {1}, root {2}, user {3}, address {4}, stream {5}, build {6}", change, client, root, user, address, branch, buildParam));
		}

		private string NoneOr(string s)
		{
			return (s == string.Empty) ? "NONE" : s;
		}

		private async Task<ulong?> PostCommit(string change, string user, string branch, string client, string commitWebhook)
		{
			CommitEmbedData commitEmbedData = new CommitEmbedData();
			commitEmbedData.change = change;
			commitEmbedData.user = user;
			commitEmbedData.branch = branch;
			commitEmbedData.client = client;

			Webhook? webhook = webhooks.Find(x => x.Name == commitWebhook);

			if (webhook == null || webhook.ID == null)
			{
				return null;
			}

			commitEmbedData.description = versionControlSystem.GetCommitDescription(change) ?? "<No description>";

			bool bIgnore = false;

			foreach (string ignorePhrase in commitIgnorePhrases)
			{
				if (commitEmbedData.description.StartsWith(ignorePhrase, StringComparison.InvariantCultureIgnoreCase))
				{
					bIgnore = true;
					break;
				}
			}

			if (bIgnore)
			{
				return null;
			}

			return await chatClient.PostCommitMessage(commitEmbedData, webhook.ID);
		}

		// --------------------------------------

		async Task OnBuildStatusUpdate(HttpContextBase context)
		{
			if (chatClient == null)
			{
				throw new Exception("Chat client was null!");
			}

			var queryParams = GetQueryParams(context);

			string jobName = queryParams["jobName"] ?? string.Empty;
			string buildIDParam = queryParams["buildID"] ?? string.Empty;
			string buildStatusParam = queryParams["buildStatus"] ?? string.Empty;
			string changeID = queryParams["changeID"] ?? string.Empty;
			string user = queryParams["user"] ?? string.Empty;

			ulong buildID;
			EBuildStatus buildStatus;

			if (jobName == string.Empty || buildStatusParam == string.Empty)
			{
				Console.WriteLine("OnBuildStatusUpdate - no jobName parameter specified, ignoring!");
				return;
			}

			if (buildIDParam == string.Empty)
			{
				Console.WriteLine("OnBuildStatusUpdate - no buildID parameter specified, ignoring!");
				return;
			}

			if (buildStatusParam == string.Empty)
			{
				Console.WriteLine("OnBuildStatusUpdate - no buildStatus parameter specified, ignoring!");
				return;
			}

			if (user == string.Empty)
			{
				Console.WriteLine("OnBuildStatusUpdate - no user parameter specified, ignoring!");
				return;
			}

			if (!ulong.TryParse(buildIDParam, out buildID)) 
			{
				Console.WriteLine($"OnBuildStatusUpdate - failed to parse a build ID from {buildIDParam}, ignoring!");
				return;
			}

			if (!Enum.TryParse<EBuildStatus>(buildStatusParam, true, out buildStatus))
			{
				Console.WriteLine($"OnBuildStatusUpdate - failed to parse a build status from {buildStatusParam}, ignoring!");
				return;
			}

			if (changeID == string.Empty)
			{
				Console.WriteLine($"OnBuldStatusUpdate - no changeID parameter specified, ignoring!");
				return;
			}

			await HandleValidBuildStatusUpdate(jobName, buildID, buildStatus, changeID, user);
		}

		// --------------------------------------

		private async Task HandleValidBuildStatusUpdate(string jobName, ulong buildID, EBuildStatus buildStatus, string changeID, string user)
		{
			BuildRecord record = new BuildRecord(jobName, buildID, changeID, user);

			List<BuildJob> matchedJobs = buildJobs.FindAll(spec => spec.Name == jobName);

			if (matchedJobs.Count == 0) 
			{
				await Log("Warning: found no build job config entries to post status for!");
				return;
			}

			if (matchedJobs.Count > 1) 
			{
				await Log("Warning: found multiple build job config entries with the same build job name, only using the first one!");
			}

			BuildJob buildJob = matchedJobs.First();

			if (buildJob.PostChannel == null) 
			{
				return;
			}

			if (buildStatus == EBuildStatus.Started)
			{
				if (RunningBuildMessages.ContainsKey(record))
				{
					await Log($"Received multiple start signals for {jobName} build {buildID}, ignoring!");
					return;
				}

				BuildStatusEmbedData embedData = new BuildStatusEmbedData();
				embedData.buildConfig = jobName;
				embedData.buildStatus = buildStatus;
				embedData.buildID = buildID;

				// TODO channel!
				ulong? message = await chatClient.PostBuildStatusEmbed(embedData, buildJob.PostChannel);

				if (message == null)
				{
					await Log("Failed to post message!");
					return;
				}
				
				RunningBuildMessages.Add(record, (ulong)message);
			}
			else
			{
				ulong runningMessage;

				if (!RunningBuildMessages.TryGetValue(record, out runningMessage))
				{
					await Log($"Received a build status update for {jobName} build {buildID} but there was no build in progress for this, ignoring!");
					return;
				}

				// TODO channel!
				await chatClient.DeleteMessage(runningMessage, buildJob.PostChannel);
			}

			/*
			if (msgID == null)
			{
				await context.Response.Send("Remote failed to run OnBuildStatusUpdate");
			}
			else
			{
				activeEmbeds.Add((ulong)msgID, null);
				await context.Response.Send("Remote ran OnBuildStatusUpdate");
			}
			*/
		}

		// --------------------------------------
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

		static void LogSync(string message)
		{
			Console.WriteLine(message);
		}

#pragma warning disable 1998
		static async Task Log(string message)
		{
			Console.WriteLine(message);
		}
	}
#pragma warning restore 1998
}
