using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.VersionControlSystems
{
	internal class GitVCS : IVersionControlSystem
	{
		public GitVCS(Config.VersionControl config)
		{
			throw new NotImplementedException();
		}

		public string? GetCommitDescription(string? change)
		{
			throw new NotImplementedException();
		}

		public string? GetStream(string? change, string? client)
		{
			throw new NotImplementedException();
		}
	}
}
