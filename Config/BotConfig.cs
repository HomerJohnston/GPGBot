using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.Config
{
	// ============================================================================================

	public class WebserverConfig
	{
		public string? Address { get; set; }
		public int? Port { get; set; }
		public string? Key { get; set; }
	}

	// ============================================================================================

	public class ChatClient
	{
		public EChatClient? System { get; set; }
		public string? ServerID { get; set; }
		public string? Token { get; set; }
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

	public class Webhook
	{
		public string? Name { get; set; }
		public string? ID { get; set; }
	}

	public class NamedWebhooks
	{
		public List<Webhook>? Webhook { get; set; }
	}

	// ============================================================================================

	public class CommitResponse
	{
		public string? Name { get; set; }
		public string? BuildJob { get; set; }
		public string? CommitWebhook { get; set; }
	}

	public class CommitResponses
	{
		public List<CommitResponse>? Stream { get; set; }
		public List<CommitResponse>? Branch { get; set; }
		
		public List<CommitResponse> Responses
		{
			get
			{
				if (Stream == null && Branch == null)
				{
					throw new Exception("Error trying to get commit responses! Bad config?");
				}

				if (Stream != null && Branch != null)
				{
					throw new Exception("Error trying to get commit responses! Both Stream and Branch used at the same time? Bad config?");
				}
#pragma warning disable 8603
				return Stream ?? Branch;
#pragma warning restore 8603
			}
		}

		public List<string>? Ignore { get; set; }
	}

	// ============================================================================================

	public class BuildJob
	{
		public string? Name { get; set; }
		public string? PostChannel { get; set; }
	}

	public class BuildJobs
	{
		public List<BuildJob>? Job { get; set; }
	}
	
	// ============================================================================================

	public class BotConfig
	{
		public WebserverConfig webserver = new();
		public ChatClient chatClient = new();
		public ContinuousIntegration ci = new();
		public VersionControl vcs = new();
		public CommitResponses commitResponses = new();
		public NamedWebhooks namedWebhooks = new();
		public BuildJobs buildJobs = new();

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

			Bind(config, "webserver", webserver);
			Bind(config, "namedWebhooks", namedWebhooks);
			Bind(config, "commitResponses", commitResponses);
			Bind(config, "buildJobs", buildJobs);
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

		private void Bind<T>(IConfigurationRoot config, string sectionName, T destination)
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
