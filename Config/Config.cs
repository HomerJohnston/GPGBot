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
		public string? Host { get; set; }
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
	
	public class Spec
	{
		public string? Branch { get; set; }
		public string? Stream { get; set; }
		public string? Build { get; set; }
		public ulong? BuildStatusChannel { get; set; }
		public string? CommitWebhook { get; set; }
	}

	public class Actions
	{
		public List<Spec>? Spec { get; set; }
	}
}
