using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.ChatClients
{
	internal interface IChatClient
	{
		/** Starts the chat client (aka bot) */
		Task Start();

		/** Stops the chat client (aka bot) */
		Task Stop();

		/** Post an embed, return a generic handle to the message */
		Task<ulong> PostBuildStatusEmbed(BuildStatusEmbedData embedData, ulong channelID = 0);// string buildConfig, ulong buildID, string status, string iconURL, Color color, string description, string changeID, string byUser);

		Task DeleteMessage(ulong messageID, ulong channelID = 0);

		Task<ulong> PostCommitMessage(CommitEmbedData embedData, string commitWebhook = "");
	}
}
