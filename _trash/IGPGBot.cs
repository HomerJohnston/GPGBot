using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebserver.Core;

namespace GPGBot
{
	internal interface IGPGBot
	{
		public Task HandleCommit();

		public void StartBuild();

		public void AbortBuild();

		public Task HandleBuildStatusUpdate();
	}
}
