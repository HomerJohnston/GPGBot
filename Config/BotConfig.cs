using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PercivalBot.Enums;
using static System.Collections.Specialized.BitVector32;

namespace PercivalBot.Config
{
	public class BotConfig
	{
		// --------------------------------------
		public WebserverConfig webserver = new();
		public ChatClient chatClient = new();
		public ContinuousIntegration ci = new();
		public VersionControl vcs = new();
		public CommitResponses commitResponses = new();
		public NamedWebhooks namedWebhooks = new();
		public BuildJobs ciJobs = new();

		// --------------------------------------
		public BotConfig(string configSource = "config.xml")
		{
			IConfigurationRoot config = new ConfigurationManager()
				.AddXmlFile(configSource, false, false)
				.Build();

			// Bind config containers & assign type
			IConfigurationSection discordSection = config.GetSection("discord");
			IConfigurationSection slackSection = config.GetSection("slack");
			TryBindExclusive(discordSection, slackSection, chatClient);
			chatClient.System = (discordSection.Exists() ? EChatClient.Discord : EChatClient.Slack);

			IConfigurationSection teamcitySection = config.GetSection("teamcity");
			IConfigurationSection jenkinsSection = config.GetSection("jenkins");
			TryBindExclusive(teamcitySection, jenkinsSection, ci);
			ci.System = (teamcitySection.Exists() ? EContinuousIntegrationSoftware.TeamCity : EContinuousIntegrationSoftware.Jenkins);

			IConfigurationSection perforceSection = config.GetSection("perforce");
			IConfigurationSection gitSection = config.GetSection("git");
			TryBindExclusive(perforceSection, gitSection, vcs);
			vcs.System = (perforceSection.Exists() ? EVersionControlSystem.Perforce : EVersionControlSystem.Git);

			TryBind(config, "webserver", webserver);
			TryBind(config, "namedWebhooks", namedWebhooks);
			TryBind(config, "commitResponses", commitResponses);
			TryBind(config, "ciJobs", ciJobs);
		}

		// --------------------------------------
		private void TryBindExclusive<T>(IConfigurationSection A, IConfigurationSection B, T destination)
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

		// --------------------------------------
		private void TryBind<T>(IConfigurationRoot config, string sectionName, T destination)
		{
			IConfigurationSection section = config.GetSection(sectionName);

			if (!section.Exists())
			{
				throw new Exception("Did not find " + section.Key.ToString() + " config!");
			}
			else
			{
				section.Bind(destination);
			}
		}
	}
}
