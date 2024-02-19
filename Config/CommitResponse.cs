using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Config
{
	public class CommitResponse
	{
		public string? Name { get; set; }
		public string? StartBuild { get; set; }
		public string? PostWebhook { get; set; }
	}
}
