using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.EmbedBuilders
{
	public interface IEmbedBuilder
	{
		string GetBuildURL(string buildConfigName);

		string GetConsoleURL(string buildConfigName, string buildID);

		string GetChangesURL(string buildConfigName, string buildID);

		Embed ConstructBuildStatusEmbed(BuildStatusEmbedData embedData);//int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName);
	}
}
