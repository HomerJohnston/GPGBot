using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.ChatClients
{
	internal class SlackClient : IChatClient
	{
		public Task DeleteMessage(ulong messageID, ulong channelID = 0)
		{
			throw new NotImplementedException();
		}

		public Task<ulong> PostBuildStatusEmbed(BuildStatusEmbedData embedData, ulong channelID = 0)
		{
			throw new NotImplementedException();
		}

		public Task<ulong> PostCommitMessage(CommitEmbedData embedData, string commitWebhook = "")
		{
			throw new NotImplementedException();
		}

		public Task Start()
		{
			throw new NotImplementedException();
		}

		public Task Stop()
		{
			throw new NotImplementedException();
		}
	}
}
