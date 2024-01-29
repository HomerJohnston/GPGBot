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

        public string GetBuildURL(string buildConfigName)
        {
			return string.Format($"{WebURL}/buildConfiguration/{buildConfigName}");
        }

        public string GetChangesURL(string buildConfigName, ulong buildID)
        {
            return string.Format($"{WebURL}/viewLog.html?buildId={buildID}&tab=buildChangesDiv");
        }

        public string GetConsoleURL(string buildConfigName, ulong buildID)
        {
			return string.Format("{0}/viewLog.html?buildId={1}", WebURL, buildID);
		}

		public Embed ConstructBuildStatusEmbed(BuildStatusEmbedData embedData)// int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName)
		{
			string buildWebURL = GetBuildURL(embedData.buildConfig);
			string buildConsoleURL = GetConsoleURL(embedData.buildConfig, embedData.buildID);
			string buildChangesURL = GetChangesURL(embedData.buildConfig, embedData.buildID);

			string authorName = string.Format("{0} Build #{1}: {2}", embedData.buildConfig, embedData.buildID, embedData.buildStatus);

			string description;

			EmbedStyle style = EmbedStyles[embedData.buildStatus];
			Color color = new(0);

			if (embedData.buildStatus == EBuildStatus.Started)
			{
				description = string.Format("Running... \u2022 [console]({0})", buildConsoleURL);
			}
			else
			{
				description = string.Format("{0} change {1} by {2} \u2022 [files]({3}) \u2022 [console]({4})", embedData.text, embedData.changeID, embedData.commitBy, buildChangesURL, buildConsoleURL);
			}

			EmbedBuilder builder = new EmbedBuilder()
				.WithAuthor(authorName, style.IconUrl, buildWebURL)
				.WithDescription(description)
				.WithColor(color);

			return builder.Build();
		}
	}
}
