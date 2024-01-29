using Discord;
using GhostPepperGames;
using System;

namespace GPGBot.EmbedBuilders.DiscordEmbedBuilders
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

        public override string GetChangesURL(string buildConfigName, ulong buildID)
        {
            return string.Format($"{WebURL}/viewLog.html?buildId={buildID}&tab=buildChangesDiv");
        }

        public override string GetConsoleURL(string buildConfigName, ulong buildID)
        {
			return string.Format($"{WebURL}/viewLog.html?buildId={buildID}&tab=buildLog");
		}
	}
}
