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
		Credential credential = null!;

		readonly string address;
		readonly string user;
		
		public PerforceVCS(Config.VersionControl config)
		{
			if (config.Address == null || config.User == null || config.Password == null)
			{
				throw new ArgumentNullException("config");
			}

			address = config.Address;
			user = config.User;

			InitializePerforce(address, user, config.Password);
		}

		public string? GetCommitDescription(string? change)
		{
			Console.WriteLine("A");
			int changeID;

			Console.WriteLine("B");
			if (int.TryParse(change, out changeID))
			{
				Console.WriteLine("C");

				if (!connection.Connect(null))
				{
					throw new Exception("Failed to connect to perforce server - is it inactive or did your ticket expire?");
				}

				Changelist x = repo.GetChangelist(changeID);
				
				Console.WriteLine(x.Description);

				return x.Description;
			}
			else 
			{
				Console.WriteLine("D");
				return "NULL";
			}
		}

		public string? GetStream(string? change, string? client)
		{
			connection.Client.Name = client;

			if (!connection.Connect(null))
			{
				throw new Exception("Failed to connect to perforce server - is it inactive or did your ticket expire?");
			}

			return connection.Client.Stream;
		}

		private Task InitializePerforce(string address, string user, string password)
		{
			server = new Server(new ServerAddress(address));
			repo = new Repository(server);
			connection = repo.Connection;

			connection.UserName = user;
			connection.Client = new Client();

			connection.Connect(null);

			// Grab a ticket. It is necessary that the bot user be assigned to a group with tickets that never expire!
			credential = connection.Login(password, null, null);

			return Task.CompletedTask;
		}
	}
}
