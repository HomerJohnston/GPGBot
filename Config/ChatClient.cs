using PercivalBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Config
{
	public class ChatClient
	{
		public EChatClient? System { get; set; }
		public string? ServerID { get; set; }
		public string? Token { get; set; }
	}
}
