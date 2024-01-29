using Discord;
using GhostPepperGames;
using System;

namespace GPGBot.EmbedBuilders.DiscordEmbedBuilders
{
    public class DiscordEmbedBuilder_Jenkins : DiscordEmbedBuilder, IEmbedBuilder
    {
        public DiscordEmbedBuilder_Jenkins(string webURL) : base(webURL)
        {
        }

        public override string GetBuildURL(string buildConfigName)
        {
            return string.Format("{0}/job/{1}", WebURL, buildConfigName);
        }

        public override string GetChangesURL(string buildConfigName, ulong buildID)
        {
            string buildURL = GetBuildURL(buildConfigName);
            string changesURL = string.Format("{0}/{1}/{2}", buildURL, buildID, "changes");

            return changesURL;
        }

        public override string GetConsoleURL(string buildConfigName, ulong buildID)
        {
            string buildURL = GetBuildURL(buildConfigName);
            string consoleURL = string.Format("{0}/{1}/{2}", buildURL, buildID, "console");

            return consoleURL;
        }
    }
}
