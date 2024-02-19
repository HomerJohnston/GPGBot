using Discord;
using GhostPepperGames;
using System;

using PercivalBot.Enums;
using PercivalBot.Structs;

using PercivalBot.ChatClients.Interface;
using PercivalBot.ChatClients.Discord;

namespace PercivalBot.ChatClients.Discord
{
    public class DiscordEmbedBuilder_Jenkins : DiscordEmbedBuilder, IEmbedBuilder
    {
        public DiscordEmbedBuilder_Jenkins(string webURL) : base(webURL)
        {
        }

        public override string GetBuildURL(string buildConfigName, string buildID)
        {
            throw new NotImplementedException();
            //return string.Format("{0}/job/{1}", WebURL, buildConfigName);
        }

        public override string GetChangesURL(string buildConfigName, string buildID)
        {
            string buildURL = GetBuildURL(buildConfigName, buildID);
            string changesURL = string.Format("{0}/{1}/{2}", buildURL, buildID, "changes");

            return changesURL;
        }

        public override string GetConsoleURL(string buildConfigName, string buildID)
        {
            string buildURL = GetBuildURL(buildConfigName, buildID);
            string consoleURL = string.Format("{0}/{1}/{2}", buildURL, buildID, "console");

            return consoleURL;
        }
    }
}
