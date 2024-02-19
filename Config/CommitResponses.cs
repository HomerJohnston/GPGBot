using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Config
{
	public class CommitResponses
	{
		public List<CommitResponse>? Stream { get; set; }
		public List<CommitResponse>? Branch { get; set; }

		public List<CommitResponse> Responses
		{
			get
			{
				if (Stream == null && Branch == null)
				{
					throw new Exception("Error trying to get commit responses! Bad config?");
				}

				if (Stream != null && Branch != null)
				{
					throw new Exception("Error trying to get commit responses! Both Stream and Branch used at the same time? Bad config?");
				}
#pragma warning disable 8603
				return Stream ?? Branch;
#pragma warning restore 8603
			}
		}

		public List<string>? Ignore { get; set; }
	}
}
