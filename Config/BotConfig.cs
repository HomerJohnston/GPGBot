using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.Config
{
	// ============================================================================================

	public class Webserver
	{
		public string? Address { get; set; }
		public int? Port { get; set; }
		public string? Key { get; set; }
	}

	// ============================================================================================

	public class ChatClient
	{
		public EChatClient? System { get; set; }
		public string? Token { get; set; }
		public ulong? defaultBuildStatusChannel { get; set; }
		public string? defaultCommitWebhook { get; set; }
	}

	// ============================================================================================

	public class ContinuousIntegration
	{
		public EContinuousIntegrationSoftware System { get; set; }
		public string? Address { get; set; }
		public string? User { get; set; }
		public string? Password { get; set; }
		public string? Token { get; set; }
	}

	// ============================================================================================

	public class VersionControl
	{
		public EVersionControlSystem System { get; set; }
		public string? Address { get; set; }
		public string? User { get; set; }
		public string? Password { get; set; }
	}

	// ============================================================================================

	public class ActionSpec
	{
		public string? Branch { get; set; }
		public string? Stream { get; set; }
		public string? BuildConfigName { get; set; }
		public string? BuildPostChannel { get; set; }
		public string? CommitWebhook { get; set; }
	}

	public class Actions
	{
		public List<ActionSpec>? Spec { get; set; }
	}

	public class BotConfig
	{
		public Webserver webserver = new();
		public ChatClient chatClient = new();
		public ContinuousIntegration ci = new();
		public VersionControl vcs = new();
		public Actions actions = new();

		public BotConfig(string configSource = "config.xml")
		{
			IConfigurationRoot config = new ConfigurationManager()
				.AddXmlFile(configSource, false, false)
				.Build();

			// Bind config containers & assign type
			IConfigurationSection discordSection = config.GetSection("discord");
			IConfigurationSection slackSection = config.GetSection("slack");
			Bind(discordSection, slackSection, chatClient);
			chatClient.System = (discordSection.Exists() ? EChatClient.Discord : EChatClient.Slack);

			IConfigurationSection teamcitySection = config.GetSection("teamcity");
			IConfigurationSection jenkinsSection = config.GetSection("jenkins");
			Bind(teamcitySection, jenkinsSection, ci);
			ci.System = (teamcitySection.Exists() ? EContinuousIntegrationSoftware.TeamCity : EContinuousIntegrationSoftware.Jenkins);

			IConfigurationSection perforceSection = config.GetSection("perforce");
			IConfigurationSection gitSection = config.GetSection("git");
			Bind(perforceSection, gitSection, vcs);
			vcs.System = (perforceSection.Exists() ? EVersionControlSystem.Perforce : EVersionControlSystem.Git);

			IConfigurationSection webserverSection = config.GetSection("webserver");
			Bind(webserverSection, webserver);

			IConfigurationSection actionsSection = config.GetSection("actions");
			Bind(actionsSection, actions);
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
	}
}
