using Discord;
using Discord.WebSocket;
using Perforce.P4;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

using WatsonWebserver;
using WatsonWebserver.Core;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using WatsonWebserver.Extensions.HostBuilderExtension;
using System.Threading;
using GPGBot.CommandHandlers;
using GPGBot.ContinuousIntegration;
using GPGBot.VersionControlSystems;
using System.Runtime.InteropServices;
using GPGBot.ChatClients;
using GPGBot.EmbedBuilders;

namespace GPGBot
{
    public class Program
	{
		#region Main
		// ========================================================================================
		// Main variables
		// ========================================================================================
//#if Windows // Fun fact: VS's "publish" function won't fucking use these OS directives, thanks C#, so instead I'll just assume that I will only ever debug on windows...
#if DEBUG
		[DllImport("Kernel32")]
		private static extern bool SetConsoleCtrlHandler(CloseEventHandler handler, bool add);
		private delegate bool CloseEventHandler();
#endif

		// ========================================================================================
		// Main
		// ========================================================================================
		public static async Task Main(string[] args)
		{
//#if Windows
#if DEBUG
			Console.CancelKeyPress += HandleCancelKeyPress;
			SetConsoleCtrlHandler(() => { return HandleClose(); }, true);
#endif
			Program program = new Program();
			await program.ProgramMain();

			Shutdown();
		}

//#if Windows
#if DEBUG
		static void HandleCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
		{
			Shutdown();
			e.Cancel = true;
		}
#endif

//#if Windows
#if DEBUG
		static bool HandleClose()
		{
			Shutdown();
			return false;
		}
#endif

		static void Shutdown()
		{
			bot?.Stop();
		}

#endregion
		// ========================================================================================
		// Settings 
		// ========================================================================================
		readonly string configSource = "config.xml";

		// ========================================================================================
		// State
		// ========================================================================================
		static GPGBot? bot;

		// ========================================================================================
		// API
		// ========================================================================================
		async Task ProgramMain()
		{
			IConfigurationRoot config = new ConfigurationManager()
				.AddXmlFile(configSource, false, false)
				.Build();

			Config.Webserver webserverConfig = new();
			Config.ChatClient chatClientConfig = new();
			Config.ContinuousIntegration continuousIntegrationConfig = new();
			Config.VersionControl versionControlConfig = new();
			Config.ActionsList actions = new();


		config.GetSection("webserver").Bind(webserverConfig);
			config.GetSection("chatClient").Bind(chatClientConfig);
			config.GetSection("continuousIntegration").Bind(continuousIntegrationConfig);
			config.GetSection("versionControl").Bind(versionControlConfig);
			config.GetSection("actions").Bind(actions);

			IVersionControlSystem vcs = GetVersionControlSystem(versionControlConfig);
			IContinuousIntegrationSystem cis = GetContinuousIntegrationSystem(continuousIntegrationConfig);
			IChatClient chatClient = GetChatClient(chatClientConfig, versionControlConfig, continuousIntegrationConfig);

			bot = new(vcs, cis, chatClient, webserverConfig);
			bot.Run();

			Console.WriteLine("\n" +
				"Bot running! To shut down: curl -H \"key:<yourkey>\" http://<botaddress>/shutdown" +
				"\n");

			await bot.runComplete.Task;
		}

		// --------------------------------------
		private IVersionControlSystem GetVersionControlSystem(Config.VersionControl config)
		{
			switch (config.System)
			{
				case EVersionControlSystem.Perforce:
				{
					return new PerforceVCS(config);
				}
				case EVersionControlSystem.Git:
				{
					return new GitVCS(config);
				}
			}

			throw new Exception("Invalid VCS specified! Check config?");
		}

		// --------------------------------------
		private IContinuousIntegrationSystem GetContinuousIntegrationSystem(Config.ContinuousIntegration config)
		{
			switch (config.System)
			{
				case EContinuousIntegrationSoftware.Jenkins:
				{
					return new JenkinsCI(config);
				}
				case EContinuousIntegrationSoftware.TeamCity:
				{
					return new TeamCityCI(config);
				}
			}

			throw new Exception("Invalid CI system specified! Check config?");
		}

		// --------------------------------------
		private IChatClient GetChatClient(Config.ChatClient chatClientConfig, Config.VersionControl vcsConfig, Config.ContinuousIntegration ciConfig)
		{
			IEmbedBuilder embedBuilder = EmbedBuilderFactory.Build(chatClientConfig, ciConfig);

			switch (chatClientConfig.System)
			{
				case EChatClient.Discord:
				{
					return new DiscordClient(chatClientConfig, embedBuilder);
				}
				case EChatClient.Slack:
				{
					throw new NotImplementedException();
				}
			}

			throw new Exception("Invalid chat client specified! Check config?");
		}
	}
}

