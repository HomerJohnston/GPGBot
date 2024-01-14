using Discord;
using GhostPepperGames;
using System;

namespace GPGBot.EmbedBuilders.DiscordEmbedBuilders
{
    internal class DiscordEmbedBuilder_Jenkins : DiscordEmbedBuilder, IEmbedBuilder
    {
        public DiscordEmbedBuilder_Jenkins(string webURL) : base(webURL)
        {
        }

        public string GetBuildURL(string buildConfigName)
        {
            return string.Format("{0}/job/{1}", WebURL, buildConfigName);
        }

        public string GetChangesURL(string buildConfigName, int buildID)
        {
            string buildURL = GetBuildURL(buildConfigName);
            string changesURL = string.Format("{0}/{1}/{2}", buildURL, buildID, "changes");

            return changesURL;
        }

        public string GetConsoleURL(string buildConfigName, int buildID)
        {
            string buildURL = GetBuildURL(buildConfigName);
            string consoleURL = string.Format("{0}/{1}/{2}", buildURL, buildID, "console");

            return consoleURL;
        }

        public Embed BuildEmbed(BuildStatusEmbedData embedData)//int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName)
        {
            string buildWebURL = GetBuildURL(embedData.buildConfig);
            string buildConsoleURL = string.Format("{0}/{1}/{2}", buildWebURL, embedData.buildID, "console");
            string buildChangesURL = string.Format("{0}/{1}/{2}", buildWebURL, embedData.buildID, "changes");

            string authorName = string.Format("{0} Build #{1}: {2}", embedData.buildConfig, embedData.buildID, embedData.buildStatus);

            string description;

			if (embedData.buildStatus == EBuildStatus.Started)
            {
                description = string.Format("Running... \u2022 [console]({0})", buildConsoleURL);

			}
            else
            {
                description = string.Format("{0} change {1} by {2} \u2022 [files]({3}) \u2022 [console]({4})", embedData.text, embedData.changeID, embedData.commitBy, buildChangesURL, buildConsoleURL);
			}

            EmbedBuilder builder = new EmbedBuilder()
                .WithAuthor(authorName, embedData.buildIconURL, buildWebURL)
                .WithDescription(description)
                .WithColor(embedData.color);

            return builder.Build();
        }
    }
}
