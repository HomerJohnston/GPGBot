using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Config
{
	public class VCSCommitResponse
	{
		public bool Ignore { get; set; }
		public string? Name { get; set; }
		public string? StartBuild { get; set; }
		public string? PostWebhook { get; set; }

		public VCSCommitResponse()
		{
			Ignore = false;
		}

		public override string ToString()
		{
			return $"CommitResponse <{Name}>: Ignore<{Ignore}>, Name<{Name}>, StartBuild<{StartBuild}>, PostWebhook<{PostWebhook}>";
		}
	}
}
