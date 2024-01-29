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
	public struct EmbedStyle
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
			{ EBuildStatus.Started, new() },
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

			config.GetSection("started").Bind(EmbedStyles[EBuildStatus.Started]);
			config.GetSection("succeeded").Bind(EmbedStyles[EBuildStatus.Started]);
			config.GetSection("failed").Bind(EmbedStyles[EBuildStatus.Started]);
			config.GetSection("unstable").Bind(EmbedStyles[EBuildStatus.Started]);
			config.GetSection("aborted").Bind(EmbedStyles[EBuildStatus.Started]);
		}
	}
}
