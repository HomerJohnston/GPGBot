using Discord;
using GhostPepperGames;
using System;

namespace GPGBot.EmbedBuilders.DiscordEmbedBuilders
{
    internal class DiscordEmbedBuilder_TeamCity : DiscordEmbedBuilder, IEmbedBuilder
    {
        public DiscordEmbedBuilder_TeamCity(string webURL) : base(webURL)
        {
        }

        public string GetBuildURL(string buildConfigName)
        {
            throw new NotImplementedException();
        }

        public string GetChangesURL(string buildConfigName, int buildID)
        {
            throw new NotImplementedException();
        }

        public string GetConsoleURL(string buildConfigName, int buildID)
        {
            throw new NotImplementedException();
        }

        public Embed BuildEmbed(BuildStatusEmbedData embedData)// int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName)
        {
            throw new NotImplementedException();
        }
	}
}
