using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
			Console.WriteLine("GetStream(" + (change ?? "NULL CHANGE") + ", " + (client ?? "NULL CLIENT") + ")");
			
			connection.Client.Name = client;

			Console.WriteLine("Connection.UserName: " + connection.UserName.ToString());
			Console.WriteLine("Connection.GetActiveTicket(): " + connection.GetActiveTicket());
			Console.WriteLine("Connection.Server.Address: " + connection.Server.Address);

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

			Console.WriteLine("Here's the connection stream we found: " + connection.Client.Stream);

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
	}
}
