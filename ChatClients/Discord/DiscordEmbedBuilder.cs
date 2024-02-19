using Discord;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PercivalBot.Enums;
using PercivalBot.Structs;

namespace PercivalBot.ChatClients.Discord
{
    // --------------------------------------------------------------------------------------------
    public class EmbedStyle
    {
        public EmbedStyle() { }

        public string IconUrl { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // --------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------
    public abstract class DiscordEmbedBuilder
    {
        // ========================================================================================
        // Settings
        // ========================================================================================
        readonly string styleSource = "embedstyles.xml";

        // ========================================================================================
        // State
        // ========================================================================================

        private string _webURL = string.Empty;
        public string WebURL
        {
            get
            {
                if (_webURL == string.Empty)
                {
                    throw new Exception("No webURL was set for the embed builder!");
                }
                else
                {
                    return _webURL;
                }
            }
            set { _webURL = value; }
        }

        protected Dictionary<EBuildStatus, EmbedStyle> EmbedStyles = new Dictionary<EBuildStatus, EmbedStyle>()
        {
            { EBuildStatus.Running, new() },
            { EBuildStatus.Succeeded, new() },
            { EBuildStatus.Failed, new() },
            { EBuildStatus.Unstable, new() },
            { EBuildStatus.Aborted, new() },
        };

        protected DiscordEmbedBuilder(string webURL)
        {
            WebURL = webURL;

            IConfigurationRoot config = new ConfigurationManager()
                .AddXmlFile(styleSource, false, false)
                .Build();

            config.GetSection("running").Bind(EmbedStyles[EBuildStatus.Running]);
            config.GetSection("succeeded").Bind(EmbedStyles[EBuildStatus.Succeeded]);
            config.GetSection("failed").Bind(EmbedStyles[EBuildStatus.Failed]);
            config.GetSection("unstable").Bind(EmbedStyles[EBuildStatus.Unstable]);
            config.GetSection("aborted").Bind(EmbedStyles[EBuildStatus.Aborted]);
        }

        public virtual string GetBuildURL(string buildConfigName)
        {
            return string.Empty;
        }

        public virtual string GetChangesURL(string buildConfigName, string buildID)
        {
            return string.Empty;
        }

        public virtual string GetConsoleURL(string buildConfigName, string buildID)
        {
            return string.Empty;
        }

        public Embed ConstructBuildStatusEmbed(BuildStatusEmbedData embedData)//int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName)
        {
            string escapedBuildConfig = Uri.EscapeDataString(embedData.buildConfig);
            string escapedBuildID = Uri.EscapeDataString(embedData.buildID.ToString());

            string buildWebURL = GetBuildURL(escapedBuildConfig);
            string buildConsoleURL = GetConsoleURL(escapedBuildConfig, escapedBuildID);
            string buildChangesURL = GetChangesURL(escapedBuildConfig, escapedBuildID);

            string authorName = string.Format("{0} Build #{1}: {2}", embedData.buildConfig, embedData.buildID, embedData.buildStatus);

            EmbedStyle style = EmbedStyles[embedData.buildStatus];
            Color color = new(uint.Parse(style.Color));
            string dot = "\u2022";

            EmbedBuilder builder = new EmbedBuilder()
                .WithAuthor(authorName, style.IconUrl, buildWebURL)
                .WithDescription($"{style.Description} change {embedData.changeID} {dot} [changes]({buildChangesURL}) {dot} [log]({buildConsoleURL})")
                .WithColor(color);

            return builder.Build();
        }
    }
}
