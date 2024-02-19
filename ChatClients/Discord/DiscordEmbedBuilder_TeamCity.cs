using Discord;
using GhostPepperGames;
using PercivalBot.ChatClients.Discord;
using PercivalBot.ChatClients.Interface;
using System;

namespace PercivalBot.ChatClients.Discord
{
    public class DiscordEmbedBuilder_TeamCity : DiscordEmbedBuilder, IEmbedBuilder
    {
        public DiscordEmbedBuilder_TeamCity(string webURL) : base(webURL)
        {
        }

        public override string GetBuildURL(string buildConfigName)
        {
            return string.Format($"{WebURL}/buildConfiguration/{buildConfigName}");
        }

        public override string GetChangesURL(string buildConfigName, string buildID)
        {
            string buildIDURL = GetBuildIDURL(buildConfigName, buildID);
            return $"{buildIDURL}?buildTab=changes&showFiles=true&expandRevisionsSection=false";
        }

        public override string GetConsoleURL(string buildConfigName, string buildID)
        {
            string buildIDURL = GetBuildIDURL(buildConfigName, buildID);
            return $"{buildIDURL}?buildTab=log&showFiles=true&expandRevisionsSection=false";
        }

        private string GetBuildIDURL(string buildConfigName, string buildID)
        {
            string buildURL = GetBuildURL(buildConfigName);
            return $"{buildURL}/{buildID}";
        }
    }
}
