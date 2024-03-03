using PercivalBot.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PercivalBot.ChatClients.Interface;

using PercivalBot.Enums;
using PercivalBot.Structs;
using PercivalBot.ChatClients.Discord;
using PercivalBot.ChatClients.Slack;

namespace PercivalBot.ChatClients
{
    public class EmbedBuilderFactory
    {
        public static IEmbedBuilder Build(ChatClientConfig chatConfig, ContinuousIntegrationConfig ciConfig)
        {
            if (ciConfig.Address == null)
            {
                throw new Exception("CI Address was null! Check config?");
            }

            switch (chatConfig.System)
            {
                case EChatClient.Discord:
                {
                    switch (ciConfig.System)
                    {
                        case EContinuousIntegrationSoftware.Jenkins:
                        {
                            return new EmbedBuilder_Discord_Jenkins(ciConfig.Address);
                        }
                        case EContinuousIntegrationSoftware.TeamCity:
                        {
                            return new EmbedBuilder_Discord_TeamCity(ciConfig.Address);
                        }
                        default:
                        {
                            throw new Exception("CI Software was invalid! Check config?");
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
