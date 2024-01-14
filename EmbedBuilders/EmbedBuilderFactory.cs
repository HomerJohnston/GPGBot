using GPGBot.EmbedBuilders.DiscordEmbedBuilders;
using GPGBot.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.EmbedBuilders
{
	internal class EmbedBuilderFactory
	{
		public static IEmbedBuilder Build(Config.ChatClient chatConfig, Config.ContinuousIntegration ciConfig)
		{
			if (ciConfig.Address == null)
			{
				throw (new Exception("CI Address was null! Check config?"));
			}

			switch (chatConfig.Name)
			{
				case EChatClient.Discord:
				{
					switch (ciConfig.Name)
					{
						case EContinuousIntegrationSoftware.Jenkins:
						{
							return new DiscordEmbedBuilder_Jenkins(ciConfig.Address);
						}
						case EContinuousIntegrationSoftware.TeamCity:
						{
							return new DiscordEmbedBuilder_TeamCity(ciConfig.Address);
						}
						default:
						{
							throw (new Exception("CI Software was invalid! Check config?"));
						}
					}
				}
				case EChatClient.Slack:
				{
					throw new NotImplementedException();
				}
				default:
				{
					throw new Exception("Chat client was invalid! Check config?");
				}
			}

		}
	}
}
