using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.ChatClients
{
	public class SlackClient : IChatClient
	{
		public Task DeleteMessage(ulong messageID, string channelName)
		{
			throw new NotImplementedException();
		}

		public Task<ulong?> PostBuildStatusEmbed(BuildStatusEmbedData embedData, string channelName)
		{
			throw new NotImplementedException();
		}

		public Task<ulong?> PostCommitMessage(CommitEmbedData embedData, string commitWebhook)
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
