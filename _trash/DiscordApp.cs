using Discord;
using Discord.WebSocket;
using GPGBot.EmbedBuilders;
using Perforce.P4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot
{
    internal class DiscordApp
	{
		readonly DiscordSocketClient discordBot = new();

		//IEmbedBuilder? ciEmbedBuilder;

		readonly Dictionary<int, BuildRecord> buildRecords = new();

		public DiscordApp()//BotAppConfig inBotSettings, IEmbedBuilder inCIEmbedBuilder)
		{
			//botSettings = inBotSettings;
			//ciEmbedBuilder = inCIEmbedBuilder;
		}

		public async Task Run()
		{
			await Task.Delay(-1);
		}

		/*
		private async Task InitializeDiscordBot()
		{
			if (discordBot.LoginState != LoginState.LoggedOut)
			{
				await Log(new LogMessage(LogSeverity.Warning, "StartBot", "Bot was already started! Stopping and restarting."));
				await discordBot.StopAsync();
			}

			discordBot.Log += Log;
			await discordBot.LoginAsync(TokenType.Bot, botConfig.Token);
			await discordBot.StartAsync();
		}

		// --------------------------------------
		private Task InitializePerforce()
		{
			Server p4server = new Server(new ServerAddress(""));
			Repository repo = new Repository(p4server);
			Connection connection = repo.Connection;

			connection.UserName = "Admin";
			connection.Client = new Client();
			//connection.Client.Name = Workspace Name

			connection.Connect(null);

			Credential credential = connection.Login("", null, null);

			ServerMetaData p4info = repo.GetServerMetaData(null);
			ServerVersion version = p4info.Version;

			string release = version.Major;

			Console.WriteLine(release);

			return Task.CompletedTask;
		}

		private bool CreateBuildRecord(int newBuildID, string jobName, int changeId, string userName)
		{
			if (buildRecords.ContainsKey(newBuildID))
			{
				Log(new LogMessage(LogSeverity.Warning, "Create Build Record", "Build record already present!"));
				return false;
			}

			BuildRecord newBuildRecord = new BuildRecord();
			newBuildRecord.JobName = jobName;
			newBuildRecord.ChangeID = changeId;

			buildRecords.Add(newBuildID, newBuildRecord);

			return true;
		}

		// --------------------------------------
		private async Task PostBuildStatus()
		{
		}

		// --------------------------------------
		private async Task DeleteMessage()
		{
		}

		// --------------------------------------
		private async Task RemoveBuildRecord(int buildID)
		{
		}

		// --------------------------------------
		private async Task SendEmbed()
		{
			Embed testEmbed = BuildEmbed(-1, "title", embedConfig.Started.IconUrl, Color.Gold, "desc", 123, "user", "job");

			if (discordBot.GetChannel(serverConfig.ChannelID) is IMessageChannel messageChannel)
			{
				await messageChannel.SendMessageAsync("test text", false, testEmbed);
			}
		}

		// --------------------------------------

		// --------------------------------------
		private Task Log(LogMessage message)
		{
			Console.WriteLine(message.ToString());
			return Task.CompletedTask;
		}
		*/
	}
}
