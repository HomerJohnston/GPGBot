using Discord;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PercivalBot.Enums;
using PercivalBot.Structs;
using PercivalBot.Config;

namespace PercivalBot.ChatClients.Discord
{
	// --------------------------------------------------------------------------------------------
	public abstract class EmbedBuilder_Discord
	{
		// ========================================================================================
		// Settings
		// ========================================================================================

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

		protected EmbedBuilder_Discord(string webURL)
		{
			WebURL = webURL;
		}

		public virtual string GetBuildURL(string buildConfigName, string buildID)
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

			string buildWebURL = GetBuildURL(escapedBuildConfig, escapedBuildID);
			string buildConsoleURL = GetConsoleURL(escapedBuildConfig, escapedBuildID);
			string buildChangesURL = GetChangesURL(escapedBuildConfig, escapedBuildID);

			string authorName = string.Format("{0} Build #{1}: {2}", embedData.buildConfig, embedData.buildID, embedData.buildStatus);

			EmbedStyleConfig style = embedData.embedStyle;

			Color color = new(uint.Parse(style.Color ?? ""));
			string dot = "\u2022";

			EmbedBuilder builder = new EmbedBuilder()
				.WithAuthor(authorName, style.IconUrl, buildWebURL)
				.WithDescription($"{style.Description} change {embedData.changeID} {dot} [changes]({buildChangesURL}) {dot} [log]({buildConsoleURL})")
				.WithColor(color);

			return builder.Build();
		}
	}
}
