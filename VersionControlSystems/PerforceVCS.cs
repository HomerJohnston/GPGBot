using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Perforce;
using Perforce.P4;

namespace GPGBot.VersionControlSystems
{
	public class PerforceVCS : IVersionControlSystem
	{
		Server server = null!;
		Repository repo = null!;
		Connection connection = null!;
		Credential? credential = null;

		readonly string address;
		readonly string user;
		readonly string password;
		
		public PerforceVCS(Config.VersionControl config)
		{
			if (config.Address == null || config.User == null || config.Password == null)
			{
				throw new ArgumentNullException("config");
			}

			address = config.Address;
			user = config.User;
			password = config.Password;

			InitializePerforce(address, user, config.Password);
		}

		public string? GetCommitDescription(string? change)
		{
			int changeID;

			if (int.TryParse(change, out changeID))
			{
				if (!connection.Connect(null))
				{
					throw new Exception("Failed to connect to perforce server - is it inactive or did your ticket expire?");
				}

				Changelist x = repo.GetChangelist(changeID);
				
				return x.Description;
			}
			else 
			{
				return "NULL";
			}
		}

		public string? GetStream(string? change, string? client)
		{
			connection.Client.Name = client;
			
			try
			{
				Options o = new Options();
				bool bResult = connection.Connect(null);

				Console.WriteLine("Connected: " + bResult);
			}
			catch
			{
				Console.WriteLine("Failed to connect to perforce server - is it inactive or did your ticket expire?");
				return null;
			}

			Console.WriteLine($"Found stream {connection.Client.Stream} for client {client}");

			return connection.Client.Stream;
		}

		private Task InitializePerforce(string address, string user, string password, string? client = null)
		{
			Console.WriteLine($"Initializing perforce... {address}, {user}, {password}, {client??""}");

			server = new Server(new ServerAddress(address));
			repo = new Repository(server);
			connection = repo.Connection;

			connection.UserName = user;
			connection.Client = new Client();

			if (client != null) 
			{
				connection.Client.Name = client;
			}

			//Console.WriteLine("Connection.GetActiveTicket(): " + connection.GetActiveTicket());

			connection.Connect(null);

			Console.WriteLine("Connection.GetActiveTicket(): " + connection.GetActiveTicket());
			// Grab a ticket. It is necessary that the bot user be assigned to a group with tickets that never expire!

			credential = connection.Login(password, true);

			if (credential == null)
			{
				Console.WriteLine("Credential is null");
			}
			else
			{
				Console.WriteLine("credential.Ticket: " + credential != null ? credential.Ticket : "NULL");
			}

			Console.WriteLine("Connection.GetActiveTicket(): " + connection.GetActiveTicket());

			ServerMetaData smd = repo.GetServerMetaData(null);
			ServerVersion v = smd.Version;
			string release = v.Major;

			Console.WriteLine("Server release: " + release);

			return Task.CompletedTask;
		}

		public void GetRequiredActionsBasedOnChanges(string? change, out bool buildCode, out bool buildWwise)
		{
			buildCode = false;
			buildWwise = false;

			if (change == null)
			{
				return;
			}

			Process p = new Process();

			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.FileName = "p4";
			p.StartInfo.Arguments = $"-ztag -F \"%depotFile%|%action%\" files @={change}";

			p.Start();

			string output = p.StandardOutput.ReadToEnd();

			List<string> codeExtensions = new List<string> { ".h", ".cpp", ".cs" };
			List<string> wwiseExtensions = new List<string> { ".wwu", ".wproj" };

			foreach (string fileSpec in new LineReader(() => new StringReader(output)))
			{
				int separator = fileSpec.LastIndexOf('|');
				
				string fileName = fileSpec.Substring(0, separator);
				string action = fileSpec.Substring(separator + 1);

				string ext = Path.GetExtension(fileName);

				if (!buildCode && codeExtensions.Contains(ext))
				{
					buildCode = true;
				}

				if (!buildWwise && wwiseExtensions.Contains(ext))
				{
					buildWwise = true;
				}

				// Don't need to keep looking for anything else!
				if (buildCode && buildWwise)
				{
					break;
				}
			}
		}
	}
}
