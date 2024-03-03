using PercivalBot.Config;
using PercivalBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Structs
{
	public struct BuildStatusEmbedData
	{
		public string changeID;
		public string buildConfig;
		public string buildNumber;
		public string buildID;
		public EBuildStatus buildStatus;
		public EmbedStyleConfig embedStyle;
	}
}
