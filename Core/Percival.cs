using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using PercivalBot.Config;
using WatsonWebserver;
using WatsonWebserver.Core;
using WatsonWebserver.Extensions.HostBuilderExtension;
using System.Linq;
using Perforce.P4;
using PercivalBot.ChatClients.Interface;
using PercivalBot.ContinuousIntegration.Interface;
using PercivalBot.VersionControlSystems.Interface;

using PercivalBot.Enums;
using PercivalBot.Structs;
using Microsoft.VisualBasic;
using Discord;

namespace PercivalBot.Core
{
    public class Percival
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
        public Percival(IVersionControlSystem inVCS, IContinuousIntegrationSystem inCI, IChatClient inChatClient, BotConfig config)
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
            WebserverConfig webserverConfig = config.webserver;

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
        Webserver BuildWebServer(WebserverConfig serverConfig)
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

            Console.WriteLine("Rejected incoming request" + (incomingKey != null ? ", invalid key: " + incomingKey : ", no key"));

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

			Commit commit = new Commit(change, client, user, branch);
			
			// workaround for p4 trigger bug - no sending of stream name capability. query for it instead.
			if (!commit.HasBranch())
            {
                await Log("No branch supplied, trying to grab stream using VCS method...");
                commit.Branch = versionControlSystem.GetStream(change, client) ?? string.Empty;
            }

            if (!commit.IsValid(out string error))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                string msg = $"Commit POST was INVALID - {error}";

				await Log(msg);
                await context.Response.Send(msg);
                return;
            }

            await HandleValidCommit(context, commit);
        }

        // --------------------------------------
        async Task HandleValidCommit(HttpContextBase context, Commit commit)
		{
			await Log($"OnCommit: {commit}");

			List<CommitResponse> matchedCommits = commitResponses.FindAll(spec => spec.Name == commit.Branch);

            string commitDescription = versionControlSystem.GetCommitDescription(commit.Change) ?? "<No description>";

            foreach (string ignorePhrase in commitIgnorePhrases)
            {
                // TODO populate the ignores!
                if (commitDescription.StartsWith(ignorePhrase, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await Log($"Ignored commit trigger for change {commit.Change}; found matching commit ignore");
                    await context.Response.Send($"Ignored commit trigger for change {commit.Change}; found matching commit ignore");

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
            versionControlSystem.GetRequiredActionsBasedOnChanges(commit.Change, out containedCode, out containedWwise);

            await Log($"Change contained code: {containedCode}; Change contained wwise: {containedWwise}");

            // Simple work to avoid posting the same commit twice
            HashSet<string> commitPostedTo = new();

            foreach (CommitResponse spec in matchedCommits)
            {
                if (spec.PostWebhook != null && !commitPostedTo.Contains(spec.PostWebhook))
                {
                    string? commitWebhook = spec.PostWebhook;

                    if (commitWebhook != null)
                    {
                        await PostCommitMessage(commit.Change, commit.User, commit.Branch, commit.Client, commitWebhook, commitDescription, containedCode || containedWwise);
                        commitPostedTo.Add(spec.PostWebhook);
                    }
                }

                if (containedCode || containedWwise)
                {
                    string? jobName = spec.StartBuild;

                    if (jobName != null)
                    {
                        await Log($"Attempting to start build: {jobName} at change: {commit.Change}");
                        bool result = await continuousIntegrationSystem.StartJob(jobName, commit.Change, containedCode, containedWwise);
                        await Log($"Build {jobName} start result: {result}");
                    }
                }
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.Send($"OnCommit: change {commit.Change}, client {commit.Client}, user {commit.User}, stream/branch {commit.Branch}, buildCode {containedCode}, buildWwise {containedWwise}");
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

		#region Build Status Updates
		// --------------------------------------
		async Task OnBuildStatusUpdate(HttpContextBase context)
        {
            var queryParams = GetQueryParams(context);

            string changeID = queryParams["changeID"] ?? string.Empty;
            string jobName = queryParams["jobName"] ?? string.Empty;
            string buildNumber = queryParams["buildNumber"] ?? string.Empty;
            string buildID = queryParams["buildID"] ?? string.Empty;
            string buildStatusParam = queryParams["buildStatus"] ?? string.Empty;

            BuildStatusUpdateRequest buildStatusUpdate = new BuildStatusUpdateRequest(changeID, jobName, buildNumber, buildID, buildStatusParam);

            if (!buildStatusUpdate.IsValid(out string error))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                string msg = $"BuildStatusUpdate POST was INVALID - {error}";

				await Log(msg);
                await context.Response.Send(msg);
                return;
            }

            await HandleValidBuildStatusUpdate(context, buildStatusUpdate);
        }

        // --------------------------------------
        private async Task HandleValidBuildStatusUpdate(HttpContextBase context, BuildStatusUpdateRequest update)
        {
			await Log($"OnBuildStatusUpdate - Valid, changeID: {update.ChangeID}, jobName: {update.JobName}, buildNumber: {update.BuildNumber}, buildID: {update.BuildID}, buildStatus: {update.Status}");

            List<BuildJob> matchedJobs = buildJobs.FindAll(spec => spec.Name == update.JobName);

            if (matchedJobs.Count == 0)
            {
				await LogAndReply($"Config warning: found no build jobs named {update.JobName} in config entries to post status for!\n", HttpStatusCode.InternalServerError, context);
				return;
            }
            else if (matchedJobs.Count > 1)
            {
				await LogAndReply($"Config warning: found multiple build jobs named {update.JobName}, there must only be one - aborting!\n", HttpStatusCode.InternalServerError, context);
				return;
			}

            BuildJob buildJob = matchedJobs.First();

            if (buildJob.PostChannel == null)
            {
                await LogAndReply($"Config warning: {update.JobName} has no post channel set!", HttpStatusCode.InternalServerError, context);
                return;
            }

			BuildRecord record = new BuildRecord(update.JobName, update.BuildNumber, update.BuildID);

			if (update.Status == EBuildStatus.Running)
            {
                await PostBuildRunning(context, update, record, buildJob);
            }
            else
            {
                await PostBuildCompletion(context, update, record, buildJob);
            }

            if (!context.Response.ResponseSent)
            {
                await LogAndReply("Unknown error! Failed to post build status.", HttpStatusCode.InternalServerError, context);
            }
        }

		// --------------------------------------
		private async Task PostBuildRunning(HttpContextBase context, BuildStatusUpdateRequest update, BuildRecord record, BuildJob buildJob)
		{
            if (buildJob.PostChannel == null)
			{
                await LogAndReply($"Config warning: {buildJob.Name} has no post channel set!",  HttpStatusCode.BadRequest, context);
				return;
			}

			if (RunningBuildMessages.ContainsKey(record))
			{
                await LogAndReply($"Received multiple start signals for {update.JobName}, build {update.BuildID}, ignoring!", HttpStatusCode.BadRequest, context);
				return;
			}

			ulong? message = await PostBuildStatus(update, buildJob.PostChannel);

			if (message == null)
			{
                await LogAndReply($"Unknown error, failed to post message!", HttpStatusCode.InternalServerError, context);
				return;
			}

			RunningBuildMessages.Add(record, (ulong)message);

            await LogAndReply($"Success", HttpStatusCode.OK, context);
		}

		// --------------------------------------
		private async Task PostBuildCompletion(HttpContextBase context, BuildStatusUpdateRequest update, BuildRecord record, BuildJob buildJob)
		{
			ulong runningMessage;

			if (!RunningBuildMessages.TryGetValue(record, out runningMessage))
			{
                await LogAndReply($"Received a build status update for {update.JobName} build {update.BuildID} but there was no build in progress for this, ignoring!", HttpStatusCode.BadRequest, context);
				return;
			}

            if (buildJob.PostChannel == null)
			{
				await LogAndReply($"Config warning: {buildJob.Name} has no post channel set!", HttpStatusCode.InternalServerError, context);
				return;
			}

			await chatClient.DeleteMessage(runningMessage, buildJob.PostChannel);

			RunningBuildMessages.Remove(record);

			ulong? message = await PostBuildStatus(update, buildJob.PostChannel);

			if (message == null)
			{
                await LogAndReply($"Failed to post build status message, error unknown!", HttpStatusCode.BadRequest, context);
				return;
			}

			if (update.Status != EBuildStatus.Succeeded)
			{
				PreviousBuildMessages.TryAdd(buildJob, new List<ulong>());
				PreviousBuildMessages[buildJob].Add((ulong)message);
			}
			else
			{
				if (PreviousBuildMessages.TryGetValue(buildJob, out List<ulong>? existingMessages) && existingMessages != null)
				{
					foreach (ulong existingMessage in existingMessages)
					{
						await chatClient.DeleteMessage(existingMessage, buildJob.PostChannel);
					}

					existingMessages.Clear();
					existingMessages.Add((ulong)message);
				}
			}

			RunningBuildMessages.Add(record, (ulong)message);

			await LogAndReply($"Success", HttpStatusCode.OK, context);
		}

		// --------------------------------------
		public async Task<ulong?> PostBuildStatus(BuildStatusUpdateRequest update, string channelName)
		{
			BuildStatusEmbedData embedData = new BuildStatusEmbedData();

            embedData.changeID = update.ChangeID;
            embedData.buildConfig = update.JobName;
            embedData.buildNumber = update.BuildNumber;
            embedData.buildID = update.BuildID;
            embedData.buildStatus = update.Status;

            ulong? message = await chatClient.PostBuildStatusEmbed(embedData, channelName);

            return message;
        }
		#endregion

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
                "-----OR-----\n" +
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

		private async Task LogAndReply(string msg, HttpStatusCode code, HttpContextBase context)
		{
			await Log(msg);

			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			await context.Response.Send(msg);
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
