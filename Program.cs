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

			// Create config containers
			Config.Webserver webserverConfig = new();
			Config.ChatClient chatClientConfig = new();
			Config.ContinuousIntegration ciConfig = new();
			Config.VersionControl vcsConfig = new();
			Config.Actions actionsConfig = new();

			// Bind config containers & assign type
			IConfigurationSection discordSection = config.GetSection("discord");
			IConfigurationSection slackSection = config.GetSection("slack");
			Bind(discordSection, slackSection, chatClientConfig);
			chatClientConfig.System = (discordSection.Exists() ? EChatClient.Discord : EChatClient.Slack);

			IConfigurationSection teamcitySection = config.GetSection("teamcity");
			IConfigurationSection jenkinsSection = config.GetSection("jenkins");
			Bind(teamcitySection, jenkinsSection, ciConfig);
			ciConfig.System = (teamcitySection.Exists() ? EContinuousIntegrationSoftware.TeamCity : EContinuousIntegrationSoftware.Jenkins);

			IConfigurationSection perforceSection = config.GetSection("perforce");
			IConfigurationSection gitSection = config.GetSection("git");
			Bind(perforceSection, gitSection, vcsConfig);
			vcsConfig.System = (perforceSection.Exists() ? EVersionControlSystem.Perforce : EVersionControlSystem.Git);

			IConfigurationSection webserverSection = config.GetSection("webserver");
			Bind(webserverSection, webserverConfig);

			IConfigurationSection actionsSection = config.GetSection("actions");
			Bind(actionsSection, actionsConfig);

			// Spawn systems
			IChatClient chatClient = CreateChatClient(chatClientConfig, ciConfig);
			IVersionControlSystem vcs = CreateVersionControlSystem(vcsConfig);
			IContinuousIntegrationSystem cis = CreateContinuousIntegrationSystem(ciConfig);

			bot = new(vcs, cis, chatClient, webserverConfig, actionsConfig);
			bot.Run();

			Console.WriteLine("\n" +
				"Bot running! To shut down: curl -H \"key:<yourkey>\" http://<botaddress>/shutdown" +
				"\n");

			await bot.runComplete.Task;
		}

		private void Bind<T>(IConfigurationSection A, IConfigurationSection B, T destination)
		{
			if (A.Exists() && B.Exists())
			{
				throw new Exception("Found both " + A.Key.ToString() + " and " + B.Key.ToString() + " configs, there must only be one!");
			}
			else if (!A.Exists() && !B.Exists())
			{
				throw new Exception("Did not find either a " + A.Key.ToString() + " config or a " + B.Key.ToString() + " config!");
			}
			else
			{
				IConfigurationSection Section = (A.Exists()) ? A : B;
				Section.Bind(destination);
			}
		}

		private void Bind<T>(IConfigurationSection Section, T destination)
		{
			if (!Section.Exists())
			{
				throw new Exception("Did not find " + Section.Key.ToString() + " config!");
			}
			else
			{
				Section.Bind(destination);
			}
		}

		// --------------------------------------
		private IVersionControlSystem CreateVersionControlSystem(Config.VersionControl config)
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
		private IContinuousIntegrationSystem CreateContinuousIntegrationSystem(Config.ContinuousIntegration config)
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
		private IChatClient CreateChatClient(Config.ChatClient chatClientConfig, Config.ContinuousIntegration ciConfig)
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

