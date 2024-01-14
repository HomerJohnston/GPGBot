using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.Config
{
	// ============================================================================================

	public class App
	{
		public string? Address { get; set; }
	}

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
		public EChatClient? Name { get; set; }
		public string? Token { get; set; }
		public ulong? defaultChannelID { get; set; }
	}

	// ============================================================================================

	public class ContinuousIntegration
	{
		public EContinuousIntegrationSoftware Name { get; set; }
		public string? Address { get; set; }
		public string? User { get; set; }
		public string? Password { get; set; }
		public string? Token { get; set; }
	}

	// ============================================================================================

	public class VersionControl
	{
		public EVersionControlSystem Name { get; set; }
		public string? Address { get; set; }
		public string? User { get; set; }
		public string? Password { get; set; }
	}

	// ============================================================================================
	/*
		internal class ActionSpec
		{
		}
	*/
}
