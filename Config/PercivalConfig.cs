using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Config
{
	public class PercivalConfig
	{
		public WebserverConfig? webserver { get; set; } = new();
		public VCSCommitResponsesConfig? vcsCommitResponses { get; set; } = new();
		public NamedWebhooksConfig? namedWebhooks { get; set; } = new();
		public CIBuildResponsesConfig? ciBuildResponses { get; set; } = new();
	}
}
