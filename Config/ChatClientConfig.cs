using PercivalBot.ChatClients;
using PercivalBot.ChatClients.Discord;
using PercivalBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Config
{
	public class ChatClientConfig
	{
		public EChatClient? System { get; set; }
		public string? ServerID { get; set; }
		public string? Token { get; set; }
		public CommitStyle? CommitStyle { get; set; }
		public List<EmbedStyleConfig>? BuildEmbedStyle { get; set; }
	}
}
