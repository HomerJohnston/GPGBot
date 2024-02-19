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
using Perforce.P4;

// p4 -Ztag -F "%depotFile%|%action%" files @=93

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
		readonly List<BuildJob> buildJobs;
		readonly List<CommitResponse> commitResponses = new();
		readonly List<string> commitIgnorePhrases = new();
		readonly List<Webhook> webhooks;

		readonly Dictionary<BuildRecord, ulong> RunningBuildMessages = new();
		readonly Dictionary<BuildJob, List<ulong>> PreviousBuildMessages = new();

		// ---------------------------------------------------------------
		public Bot(IVersionControlSystem inVCS, IContinuousIntegrationSystem inCI, IChatClient inChatClient, Config.BotConfig config)
		{
			// Set up major components
			versionControlSystem = inVCS;
			continuousIntegrationSystem = inCI;
			chatClient = inChatClient;

			// Set up other config data

			if (config.commitResponses.Responses == null)
			{
				LogSync("Warning: no commit responses found in config file!");
				commitResponses = new List<CommitResponse>();
			}
			else
			{
				List<CommitResponse> fallthroughResponses = new();

				foreach (CommitResponse response in config.commitResponses.Responses)
				{
					if (response.StartBuild == null && response.PostWebhook == null)
					{
						fallthroughResponses.Add(response);
						continue;
					}
				
					commitResponses.Add(response);

					foreach (CommitResponse fallthroughResponse in fallthroughResponses)
					{
						fallthroughResponse.StartBuild = response.StartBuild;
						fallthroughResponse.PostWebhook = response.PostWebhook;
						commitResponses.Add(fallthroughResponse);
					}
				}
			}

			if (config.commitResponses.Ignore == null)
			{
				LogSync("No commit ignore phrases found in config file.");
				commitIgnorePhrases = new List<string>();
			}
			else
			{
				commitIgnorePhrases.AddRange(config.commitResponses.Ignore);
			}

			buildJobs = config.ciJobs.Build ?? new();
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

			context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
			await context.Response.Send("Request denied. Required format: curl http://botaddress -H \"key:passphrase\"");
		}
		#endregion

        // --------------------------------------
		async Task OnCommit(HttpContextBase context)
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
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await Log($"Commit POST was INVALID - something wasn't set: change {NoneOr(change)}, client {NoneOr(client)}, user {NoneOr(user)}, branch {NoneOr(branch)}");
				await context.Response.Send($"Commit POST was INVALID - something wasn't set: change {NoneOr(change)}, client {NoneOr(client)}, user {NoneOr(user)}, branch {NoneOr(branch)}");
				return;
			}

			await HandleValidCommit(context, change, client, user, branch);
		}
		
        // --------------------------------------
		async Task HandleValidCommit(HttpContextBase context, string changeID, string client, string user, string branch)
		{
			List<CommitResponse> matchedCommits = commitResponses.FindAll(spec => spec.Name == branch);

			await Log($"OnCommit: change {NoneOr(changeID)}, client {NoneOr(client)}, user {NoneOr(user)}, branch {NoneOr(branch)}");

			string commitDescription = versionControlSystem.GetCommitDescription(changeID) ?? "<No description>";

			foreach (string ignorePhrase in commitIgnorePhrases)
			{
				// TODO populate the ignores!
				if (commitDescription.StartsWith(ignorePhrase, StringComparison.OrdinalIgnoreCase))
				{
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					await Log($"Ignored commit trigger for change {changeID}; found matching commit ignore");
					await context.Response.Send($"Ignored commit trigger for change {changeID}; found matching commit ignore");

					return;
				}
			}

			if (matchedCommits.Count == 0)
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await Log($"No matching action specs found! Ignoring this commit.");
				await context.Response.Send($"No matching action specs found! Ignoring this commit.");
			}

			bool containedCode;
			bool containedWwise;
			versionControlSystem.GetRequiredActionsBasedOnChanges(changeID, out containedCode, out containedWwise);

			await Log($"Change contained code: {containedCode}; Change contained wwise: {containedWwise}");

			// Simple work to avoid posting the same commit twice
			HashSet<string> commitPostedTo = new();

			foreach (Config.CommitResponse spec in matchedCommits)
			{
				if (spec.PostWebhook != null && !commitPostedTo.Contains(spec.PostWebhook))
				{
					string? commitWebhook = spec.PostWebhook;

					if (commitWebhook != null)
					{
						await PostCommitMessage(changeID, user, branch, client, commitWebhook, commitDescription, containedCode || containedWwise);
						commitPostedTo.Add(spec.PostWebhook);
					}
				}

				if (containedCode || containedWwise)
				{
					string? jobName = spec.StartBuild;

					if (jobName != null)
					{
						await Log($"Attempting to start build: {jobName} at change: {changeID}");
						bool result = await continuousIntegrationSystem.StartJob(jobName, changeID, containedCode, containedWwise);
						await Log($"Build {jobName} start result: {result}");
					}
				}
			}

			context.Response.StatusCode = (int)HttpStatusCode.OK;
			await context.Response.Send($"OnCommit: change {changeID}, client {client}, user {user}, stream/branch {branch}, buildCode {containedCode}, buildWwise {containedWwise}");
		}

        // --------------------------------------
		private string NoneOr(string s)
		{
			return (s == string.Empty) ? "NONE" : s;
		}

        // --------------------------------------
		private async Task<ulong?> PostCommitMessage(string change, string user, string branch, string client, string commitWebhook, string description, bool doBuild)
		{
			Webhook? webhook = webhooks.Find(x => x.Name == commitWebhook);

			if (webhook == null || webhook.ID == null)
			{
				return null;
			}

			CommitEmbedData commitEmbedData = new CommitEmbedData();
			commitEmbedData.change = change;
			commitEmbedData.user = user;
			commitEmbedData.branch = branch;
			commitEmbedData.client = client;
			commitEmbedData.description = description;
			commitEmbedData.containsCode = doBuild;

			return await chatClient.PostCommitMessage(commitEmbedData, webhook.ID);
		}

		// --------------------------------------
		async Task OnBuildStatusUpdate(HttpContextBase context)
		{
			var queryParams = GetQueryParams(context);

			string changeID = queryParams["changeID"] ?? string.Empty;
			string jobName = queryParams["jobName"] ?? string.Empty;
			string buildNumber = queryParams["buildNumber"] ?? string.Empty;
			string buildID = queryParams["buildID"] ?? string.Empty;
			string buildStatusParam = queryParams["buildStatus"] ?? string.Empty;

			await Log($"OnBuildStatusUpdate, changeID: {changeID}, jobName: {jobName}, buildNumber: {buildNumber}, buildID: {buildID}, buildStatus: {buildStatusParam}");

			EBuildStatus buildStatus;

			if (changeID == string.Empty)
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send($"OnBuildStatusUpdate - no changeID parameter specified, ignoring!");
				return;
			}

			if (jobName == string.Empty || buildStatusParam == string.Empty)
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send("OnBuildStatusUpdate - no jobName parameter specified, ignoring!");
				return;
			}

			if (buildNumber == string.Empty)
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send("OnBuildStatusUpdate - no buildNumber parameter specified, ignoring!");
				return;
			}

			if (buildID == string.Empty)
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send("OnBuildStatusUpdate - no buildID parameter specified, ignoring!");
				return;
			}

			if (buildStatusParam == string.Empty)
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send("OnBuildStatusUpdate - no buildStatus parameter specified, ignoring!");
				return;
			}

			if (buildID == string.Empty) 
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send($"OnBuildStatusUpdate - no buildID parameter specified, ignoring!");
				return;
			}

			if (!EBuildStatus.TryParse(buildStatusParam, true, out buildStatus))
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send($"OnBuildStatusUpdate - failed to parse a build status from {buildStatusParam}, ignoring!");
				return;
			}

			await Log($"OnBuildStatusUpdate - Valid, changeID: {changeID}, jobName: {jobName}, buildNumber: {buildNumber}, buildID: {buildID}, buildStatus: {buildStatus}");

			await HandleValidBuildStatusUpdate(context, changeID, jobName, buildNumber, buildID, buildStatus);
		}

		// --------------------------------------
		private async Task HandleValidBuildStatusUpdate(HttpContextBase context, string changeID, string jobName, string buildNumber, string buildID, EBuildStatus buildStatus)
		{
			BuildRecord record = new BuildRecord(jobName, buildNumber, buildID);

			List<BuildJob> matchedJobs = buildJobs.FindAll(spec => spec.Name == jobName);

			string result = string.Empty;
			
			if (matchedJobs.Count == 0) 
			{
				result += $"Config warning: found no build jobs named {jobName} in config entries to post status for!";
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send(result);

				await Log($"Config warning: found no build jobs named {jobName} in config entries to post status for!");

				return;
			}

			if (matchedJobs.Count > 1) 
			{
				result += $"Config warning: found multiple build jobs named {jobName}, only using the first one!\n";
			}

			BuildJob buildJob = matchedJobs.First();

			if (buildJob.PostChannel == null) 
			{
				result += $"Config warning: {jobName} has no post channel set!";
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				await context.Response.Send(result);
				return;
			}

			if (buildStatus == EBuildStatus.Running)
			{
				if (RunningBuildMessages.ContainsKey(record))
				{
					result += $"Received multiple start signals for {jobName}, build {buildID}, ignoring!";
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					await context.Response.Send(result);
					return;
				}

				ulong? message = await PostBuildStatus(changeID, jobName, buildNumber, buildID, buildStatus, buildJob.PostChannel);

				if (message == null)
				{
					result += "Failed to post message!";
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					await context.Response.Send(result);
					return;
				}

				RunningBuildMessages.Add(record, (ulong)message);

			}
			else
			{
				ulong runningMessage;

				if (!RunningBuildMessages.TryGetValue(record, out runningMessage))
				{
					result += $"Received a build status update for {jobName} build {buildID} but there was no build in progress for this, ignoring!";
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					await context.Response.Send(result);
					return;
				}

				await chatClient.DeleteMessage(runningMessage, buildJob.PostChannel);

				RunningBuildMessages.Remove(record);

				ulong? newstatusMessage = await PostBuildStatus(changeID, jobName, buildNumber, buildID, buildStatus, buildJob.PostChannel);

				if (newstatusMessage == null)
				{
					result += $"Failed to post build status message, error unknown!";
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					await context.Response.Send(result);
					return;
				}

				if (buildStatus != EBuildStatus.Succeeded)
				{
					PreviousBuildMessages.TryAdd(buildJob, new List<ulong>());
					PreviousBuildMessages[buildJob].Add((ulong)newstatusMessage);
				}
				else
				{
					if (PreviousBuildMessages.TryGetValue(buildJob, out List<ulong>? existingMessages) && existingMessages != null)
					{
						foreach (ulong messageID in existingMessages)
						{
							await chatClient.DeleteMessage(messageID, buildJob.PostChannel);
						}

						existingMessages.Clear();
						existingMessages.Add((ulong)newstatusMessage);
					}
				}
			}

			result += "Success";
			context.Response.StatusCode = (int)HttpStatusCode.OK;
			await context.Response.Send(result);
		}

        // --------------------------------------
		public async Task<ulong?> PostBuildStatus(string changeID, string jobName, string buildNumber, string buildID, EBuildStatus buildStatus, string channelName)
		{
			BuildStatusEmbedData embedData = new BuildStatusEmbedData();
			embedData.changeID = changeID;
			embedData.buildConfig = jobName;
			embedData.buildNumber = buildNumber;
			embedData.buildID = buildID;
			embedData.buildStatus = buildStatus;

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
				"\n" +
				"Ping? Pong! This is a default response. Usage:\n" +
				"\n" +
				"curl http://botaddress:port/command -H \"key:passphrase\" -d \"param=value&param=value\"\n" +
				"-----OR-----\n"+
				"Invoke-RestMethod -Method 'POST' -Uri http://botaddress:port/command -Headers @{'key'='passphrase'} -Body @{'param'='value';'param'='value'}\n" +
				"\n" +
				"Valid commands:\n" +
				"    /on-commit            params: change=id, client=name, user=name, branch=name, build=trueOrFalse\n" +
				"    /build-status-update  params: jobName=...&buildID=...&buildStatus=running|succeeded|failed|unstable|aborted\n" +
				"    /shutdown             params: (none required)"
			);
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
