using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using GPGBot.ChatClients;
using GPGBot.ContinuousIntegration;
using GPGBot.VersionControlSystems;
using GPGBot.Config;
using WatsonWebserver;
using WatsonWebserver.Core;
using WatsonWebserver.Extensions.HostBuilderExtension;
using System.Linq;

namespace GPGBot
{
	public class Bot
	{
		// --------------------------------------
		public TaskCompletionSource<bool> runComplete = new();

		IVersionControlSystem versionControlSystem;
		
		IContinuousIntegrationSystem continuousIntegrationSystem;
		
		IChatClient chatClient;

		WebserverBase webserver;
		string? webserverKey;

		// --------------------------------------
		readonly List<Config.BuildJob> buildJobs;
		readonly List<Config.CommitResponse> commitResponses;
		readonly List<string> commitIgnorePhrases = new();
		readonly List<Config.Webhook> webhooks;

		Dictionary<BuildRecord, ulong> RunningBuildMessages = new();

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
				.MapParameteRoute(HttpMethod.POST, "/on-commit", OnCommitContent, true) // Handles triggers from source control system
				.MapParameteRoute(HttpMethod.POST, "/on-commit-code", OnCommitCode, true) // Handles triggers from source control system
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

			context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
			await context.Response.Send("Request denied. Required format: curl http://botaddress -H \"key:passphrase\"");
		}
		#endregion

		// --------------------------------------
		async Task OnCommitContent(HttpContextBase context)
		{
			await Log("OnCommitContent");
			await OnCommit(context, false);
		}

		// --------------------------------------
		async Task OnCommitCode(HttpContextBase context)
		{
			await Log("OnCommitCode");
			await OnCommit(context, true);
		}

		async Task OnCommit(HttpContextBase context, bool doBuild)
		{
			var queryParams = GetQueryParams(context);

			string change = queryParams["change"] ?? string.Empty;
			string client = queryParams["client"] ?? string.Empty;
			string user = queryParams["user"] ?? string.Empty;
			string branch = queryParams["branch"] ?? string.Empty; // branch is used for potential git compatibility only; p4 triggers %stream% is bugged and cannot send stream name

			// workaround for p4 trigger bug - no sending of stream name capability. query for it instead.
			if (branch == string.Empty)
			{
				branch = versionControlSystem.GetStream(change, client) ?? string.Empty;
			}

			if (change == string.Empty || client == string.Empty || user == string.Empty || branch == string.Empty)
			{
				Console.WriteLine($"OnCommit INVALID - something wasn't set: change {NoneOr(change)}, client {NoneOr(client)}, user {NoneOr(user)}, branch {NoneOr(branch)}, build {doBuild}");
				return;
			}

			await HandleValidCommit(context, change, client, user, branch, doBuild);
		}

		async Task HandleValidCommit(HttpContextBase context, string change, string client, string user, string branch, bool doBuild)
		{
			List<Config.CommitResponse> matchedCommits = commitResponses.FindAll(spec => spec.Name == branch);

			Console.WriteLine($"OnCommit: change {NoneOr(change)}, client {NoneOr(client)}, user {NoneOr(user)}, branch {NoneOr(branch)}, build {doBuild}");

			if (matchedCommits.Count == 0)
			{
				context.Response.StatusCode = 204;
				await context.Response.Send(string.Format($"No matching action specs found! Ignoring this commit."));
				Console.WriteLine("No matching action specs found! Ignoring this commit.");
				return;
			}

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

				if (doBuild)
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
			await context.Response.Send($"OnCommit: change {change}, client {client}, user {user}, stream/branch {branch}, build {doBuild}");
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
			var queryParams = GetQueryParams(context);

			string jobName = queryParams["jobName"] ?? string.Empty;
			string buildIDParam = queryParams["buildID"] ?? string.Empty;
			string buildStatusParam = queryParams["buildStatus"] ?? string.Empty;

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

			if (!ulong.TryParse(buildIDParam, out buildID)) 
			{
				Console.WriteLine($"OnBuildStatusUpdate - failed to parse a build ID from {buildIDParam}, ignoring!");
				return;
			}

			if (!EBuildStatus.TryParse(buildStatusParam, true, out buildStatus))
			{
				Console.WriteLine($"OnBuildStatusUpdate - failed to parse a build status from {buildStatusParam}, ignoring!");
				return;
			}

			string result =	await HandleValidBuildStatusUpdate(jobName, buildID, buildStatus);

			await context.Response.Send(result);
		}

		// --------------------------------------

		private async Task<string> HandleValidBuildStatusUpdate(string jobName, ulong buildID, EBuildStatus buildStatus)
		{
			BuildRecord record = new BuildRecord(jobName, buildID);

			List<BuildJob> matchedJobs = buildJobs.FindAll(spec => spec.Name == jobName);

			string result = string.Empty;

			if (matchedJobs.Count == 0) 
			{
				result += $"Config warning: found no build jobs named {jobName} in config entries to post status for!";
				return result;
			}

			if (matchedJobs.Count > 1) 
			{
				result += $"Config warning: found multiple build jobs named {jobName}, only using the first one!";
			}

			BuildJob buildJob = matchedJobs.First();

			if (buildJob.PostChannel == null) 
			{
				result += $"Config warning: {jobName} has no post channel set!";
				return result;
			}

			if (buildStatus == EBuildStatus.Started)
			{
				if (RunningBuildMessages.ContainsKey(record))
				{
					result += $"Received multiple start signals for {jobName}, build {buildID}, ignoring!";
					return result;
				}

				ulong? message = await PostBuildStatus(jobName, buildID, buildStatus, buildJob.PostChannel);

				if (message == null)
				{
					result += "Failed to post message!";
					return result;
				}
				
				RunningBuildMessages.Add(record, (ulong)message);
			}
			else
			{
				ulong runningMessage;

				if (!RunningBuildMessages.TryGetValue(record, out runningMessage))
				{
					result += $"Received a build status update for {jobName} build {buildID} but there was no build in progress for this, ignoring!";
					return result;
				}

				await chatClient.DeleteMessage(runningMessage, buildJob.PostChannel);

				RunningBuildMessages.Remove(record);

				await PostBuildStatus(jobName, buildID, buildStatus, buildJob.PostChannel);
			}

			result += result.Length == 0 ? "Success" : "\nSuccess";
			return result;
		}

		public async Task<ulong?> PostBuildStatus(string jobName, ulong buildID, EBuildStatus buildStatus, string channelName)
		{
			BuildStatusEmbedData embedData = new BuildStatusEmbedData();
			embedData.buildConfig = jobName;
			embedData.buildStatus = buildStatus;
			embedData.buildID = buildID;

			// TODO channel!
			ulong? message = await chatClient.PostBuildStatusEmbed(embedData, channelName);

			return message;
		}

		// --------------------------------------
		public async Task Shutdown(HttpContextBase context)
		{
			await Log("Shutting down!");

			await context.Response.Send("Bot: shutting down!");

			runComplete.SetResult(true);
		}

		// --------------------------------------
		async Task DefaultRoute(HttpContextBase context)
		{
			context.Response.StatusCode = (int)HttpStatusCode.OK;
			await context.Response.Send(
				"Usage:\n" +
				"curl http://botaddress -H \"key:passphrase\" -d \"param=value&param=value\"\n" +
				"\n" +
				"Valid commands:\n" +
				"    /on-commit            params: change=id, client=name, user=name, branch=name, build=trueOrFalse\n" +
				"    /build-status-update  params: jobName=...&buildID=...&buildStatus=started|succeeded|failed|unstable|aborted\n" +
				"    /shutdown\n\n" +

				"Example:\n" +
				"curl -H \"key:passphrase\" -d ");
		}
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
