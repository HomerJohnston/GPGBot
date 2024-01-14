using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.EmbedBuilders
{
	internal interface IEmbedBuilder
	{
		string GetBuildURL(string buildConfigName);

		string GetConsoleURL(string buildConfigName, int buildID);

		string GetChangesURL(string buildConfigName, int buildID);

		Embed BuildEmbed(BuildStatusEmbedData embedData);//int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName);
	}
}
