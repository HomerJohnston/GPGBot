using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using GPGBot.EmbedBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.ChatClients
{
	internal class DiscordClient : IChatClient
	{
		readonly Config.ChatClient config;

		readonly DiscordSocketClient client;

		readonly IEmbedBuilder embedBuilder;

		readonly ulong defaultChannelID;

		readonly string defaultCommitWebhook;

		public DiscordClient(Config.ChatClient inConfig, IEmbedBuilder inEmbedBuilder)
		{
			config = inConfig;
			embedBuilder = inEmbedBuilder;
			defaultChannelID = inConfig.defaultBuildStatusChannel ?? 0;
			defaultCommitWebhook = inConfig.defaultCommitWebhook ?? string.Empty;

			DiscordSocketConfig clientConfig = new DiscordSocketConfig();
			client = new DiscordSocketClient(clientConfig);
		}

		public async Task Start()
		{
			if (config == null)
			{
				throw new Exception("DiscordClient config was null!");
			}

			client.Log += Log;
			
			await client.LoginAsync(TokenType.Bot, config.Token);
			await client.StartAsync();
		}

		public async Task Stop()
		{
            if (client != null)
            {
				await client.LogoutAsync();
				await client.StopAsync();
			}
		}

		private async Task Log(LogMessage message)
		{
			Console.WriteLine(message.ToString());

			await Task.CompletedTask;
		}

		public async Task<ulong> PostBuildStatusEmbed(BuildStatusEmbedData embedData, ulong channelID)
		{
			if (channelID == 0)
			{
				channelID = defaultChannelID;
			}

			Embed e = embedBuilder.ConstructBuildStatusEmbed(embedData);

			if (client.GetChannel(channelID) is IMessageChannel messageChannel)
			{
				IUserMessage sentMessage = await messageChannel.SendMessageAsync("Test Text!", false, e);
				return sentMessage.Id;
			}
			
			return 0;
		}

		public async Task DeleteMessage(ulong messageID, ulong channelID = 0)
		{
			if (channelID == 0)
			{
				channelID = defaultChannelID;
			}

			// TODO: these probably don't need to crash my program. Log errors instead.
			if (channelID == 0)
			{
				throw new Exception("Invalid channel ID!");
			}

			if (client.GetChannel(channelID) is IMessageChannel messageChannel)
			{
				RequestOptions options = new RequestOptions();
				await messageChannel.DeleteMessageAsync(messageID);
			}
			else
			{
				throw new Exception($"Unable to delete message {messageID} from channel {channelID} (channel not found)");
			}

			await Task.CompletedTask;
		}

		public async Task<ulong> PostCommitMessage(CommitEmbedData embedData, string commitWebhook = "")
		{
			if (commitWebhook == string.Empty)
			{
				commitWebhook = defaultCommitWebhook;
			}

			Console.WriteLine("Posting commit message to... " + commitWebhook);

			DiscordWebhookClient webhookClient = new(commitWebhook);

			webhookClient.Log += LogTest;

			Console.WriteLine("Created webhook client");

			string titleText = string.Format("Change {0}  \u2022  {1}", embedData.change, embedData.client);

			Console.WriteLine(titleText);

			EmbedBuilder builder = new EmbedBuilder()
				.WithAuthor(titleText, "https://i.imgur.com/TzA17kl.png")
				.WithDescription(embedData.description);

			Embed e = builder.Build();

			List<Embed> embeds = new List<Embed>() { e };

			return await webhookClient.SendMessageAsync(null, false, embeds);


			/*
			EmbedBuilder embedBuilder = new EmbedBuilder()
				.WithAuthor(embedData.user);

			Embed e = embedBuilder.Build();

			if (client.GetChannel(channelID) is IMessageChannel channel)
			{
				IUserMessage sentMessage = await channel.SendMessageAsync("Test text!", false, e);

				return sentMessage.Id;
			}

			return 0;
			*/
		}

		private Task LogTest(LogMessage message)
		{
			Console.WriteLine(message.ToString());

			return Task.CompletedTask;
		}
	}
}
