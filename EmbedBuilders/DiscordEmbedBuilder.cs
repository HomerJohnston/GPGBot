using Discord;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot
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

		public virtual string GetChangesURL(string buildConfigName, ulong buildID)
		{
			return string.Empty;
		}

		public virtual string GetConsoleURL(string buildConfigName, ulong buildID)
		{
			return string.Empty;
		}

		public Embed ConstructBuildStatusEmbed(BuildStatusEmbedData embedData)//int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName)
		{
			string buildWebURL = GetBuildURL(embedData.buildConfig);
			string buildConsoleURL = GetConsoleURL(embedData.buildConfig, embedData.buildID);
			string buildChangesURL = GetChangesURL(embedData.buildConfig, embedData.buildID);

			string? buildWebURLFixed = buildWebURL.Replace(" ", "%20");// System.Web.HttpUtility.UrlEncode(buildWebURL);

			if (buildWebURLFixed == null)
			{
				throw new Exception("Invalid URL!");
			}
			else
			{
				Console.WriteLine(buildWebURLFixed);
			}


			string authorName = string.Format("{0} Build #{1}: {2}", embedData.buildConfig, embedData.buildID, embedData.buildStatus);

			string description;

			EmbedStyle style = EmbedStyles[embedData.buildStatus];
			Color color = new(UInt32.Parse(style.Color));

			description = string.Format($"{embedData.text} change {embedData.changeID} \u2022 [changes]({buildChangesURL}) \u2022 [log]({buildConsoleURL})");

			EmbedBuilder builder = new EmbedBuilder()
				.WithAuthor(authorName, style.IconUrl, buildWebURLFixed)
				.WithDescription(description)
				.WithColor(color);

			return builder.Build();
		}
	}
}
