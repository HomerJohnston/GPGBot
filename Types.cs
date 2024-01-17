using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot
{
	public enum EChatClient
	{
		NULL,
		Discord,
		Slack,
	}

	public enum EContinuousIntegrationSoftware
	{
		NULL,
		Jenkins,
		TeamCity,
	}

	public enum EVersionControlSystem
	{
		NULL,
		Perforce,
		Git,
	}

	public enum EBuildStatus
	{
		NULL,
		Started,
		Succeeded,
		Failed,
		Unstable,
		Aborted
	}

	public struct BuildStatusEmbedData
	{
		public string buildConfig;
		public ulong buildID;
		public EBuildStatus buildStatus;
		public string buildIconURL;
		public Discord.Color color;
		public string text;
		public string changeID;
		public string commitBy;
	}

	public struct CommitEmbedData
	{
		public string change;
		public string stream;
		public string user;
		public string client;
		public string description;
	}
}
