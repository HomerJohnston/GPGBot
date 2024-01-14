using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace GhostPepperGames
{

	// ============================================================================================
	/*
	internal class EmbedConfig
	{
		public readonly string styleSource = "EmbedStyles.xml";

		Dictionary<EEmbedStyleName, EmbedStyle> EmbedStyles = new Dictionary<EEmbedStyleName, EmbedStyle>()
		{
			{ EEmbedStyleName.Started, new() },
			{ EEmbedStyleName.Succeeded, new() },
			{ EEmbedStyleName.Failed, new() },
			{ EEmbedStyleName.Unstable, new() },
			{ EEmbedStyleName.Aborted, new() },
		};

		public void Load()
		{
			IConfigurationRoot config = new ConfigurationManager()
				.AddXmlFile(styleSource, false, false)
				.Build();

			config.GetSection("started").Bind(EmbedStyles[EEmbedStyleName.Started]);
			config.GetSection("succeeded").Bind(EmbedStyles[EEmbedStyleName.Started]);
			config.GetSection("failed").Bind(EmbedStyles[EEmbedStyleName.Started]);
			config.GetSection("unstable").Bind(EmbedStyles[EEmbedStyleName.Started]);
			config.GetSection("aborted").Bind(EmbedStyles[EEmbedStyleName.Started]);
		}
	}
	*/
}
