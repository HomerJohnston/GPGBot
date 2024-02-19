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

        public override string GetBuildURL(string buildConfigName, string buildID)
        {
            return string.Format($"{WebURL}/buildConfiguration/{buildConfigName}/{buildID}");
        }

        public override string GetChangesURL(string buildConfigName, string buildID)
        {
            string buildIDURL = GetBuildURL(buildConfigName, buildID);
            return $"{buildIDURL}?buildTab=changes&showFiles=true&expandRevisionsSection=false";
        }

        public override string GetConsoleURL(string buildConfigName, string buildID)
        {
            string buildIDURL = GetBuildURL(buildConfigName, buildID);
            return $"{buildIDURL}?buildTab=log&showFiles=true&expandRevisionsSection=false";
        }
    }
}
