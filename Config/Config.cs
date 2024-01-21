using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.Config
{
	// ============================================================================================

	public class Webserver
	{
		public int? Port { get; set; }
		public string? Key { get; set; }
	}

	// ============================================================================================

	public class ChatClient
	{
		public EChatClient? System { get; set; }
		public string? Token { get; set; }
		public ulong? defaultBuildStatusChannel { get; set; }
		public string? defaultCommitWebhook { get; set; }
	}

	// ============================================================================================

	public class ContinuousIntegration
	{
		public EContinuousIntegrationSoftware System { get; set; }
		public string? Address { get; set; }
		public string? User { get; set; }
		public string? Password { get; set; }
		public string? Token { get; set; }
	}

	// ============================================================================================

	public class VersionControl
	{
		public EVersionControlSystem System { get; set; }
		public string? Address { get; set; }
		public string? User { get; set; }
		public string? Password { get; set; }
	}

	// ============================================================================================

	public class ActionSpec
	{
		public string? Branch { get; set; }
		public string? Stream { get; set; }
		public string? BuildConfigName { get; set; }
		public ulong? BuildPostChannel { get; set; }
		public string? CommitWebhook { get; set; }
	}

	public class WTF
	{
		public List<ActionSpec?>? Spec { get; set; }
	}

	public class Actions
	{
		public string? TestData { get; set; }
		public WTF? Specs { get; set; }
	}
}
