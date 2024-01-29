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
	public class DiscordClient : IChatClient
	{
		readonly Config.ChatClient config;

		readonly DiscordSocketClient client;

		readonly IEmbedBuilder embedBuilder;

		readonly ulong defaultChannelID;

		readonly string defaultCommitWebhook;

		bool clientReady = false;

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

			client.Ready += OnClientReady;

			await client.LoginAsync(TokenType.Bot, config.Token);
			await client.StartAsync();

			while (!clientReady)
			{
				await Task.Delay(50);
			}
		}

		private Task OnClientReady()
		{
			clientReady = true;
			return Task.CompletedTask;
		}

		public async Task Stop()
		{
			clientReady = false;

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

		public async Task<ulong?> PostBuildStatusEmbed(BuildStatusEmbedData embedData, ulong? channelID = null)
		{
			if (channelID == null)
			{
				channelID = defaultChannelID;
			}

			IChannel channel = client.GetChannel((ulong)channelID);

			if (channel == null)
			{
				Console.WriteLine($"channel {channelID} not found!");
			}
			else
			{
				Console.WriteLine($"channel {channelID} found!");
			}

			if (channel is IMessageChannel messageChannel)
			{
				Embed e = embedBuilder.ConstructBuildStatusEmbed(embedData);
				IUserMessage sentMessage = await messageChannel.SendMessageAsync(null, false, e);
				return sentMessage.Id;
			}

			return null;
		}

		public async Task DeleteMessage(ulong messageID, ulong? channelID = null)
		{
			if (channelID == null)
			{
				channelID = defaultChannelID;
			}

			// TODO: these probably don't need to crash my program. Log errors instead.
			if (channelID == 0)
			{
				throw new Exception("Invalid channel ID!");
			}

			if (client.GetChannel((ulong)channelID) is IMessageChannel messageChannel)
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

		public async Task<ulong?> PostCommitMessage(CommitEmbedData embedData, string? commitWebhook = null)
		{
			if (commitWebhook == null || commitWebhook == string.Empty)
			{
				commitWebhook = defaultCommitWebhook;
			}

			DiscordWebhookClient webhookClient = new(commitWebhook);

			webhookClient.Log += ConsoleLog;

			string titleText = string.Format("Change {0}  \u2022  {1}", embedData.change, embedData.client);

			EmbedBuilder builder = new EmbedBuilder()
				.WithAuthor(titleText, "https://i.imgur.com/TzA17kl.png")
				.WithDescription(embedData.description)
				.WithColor(1094111);

			Embed e = builder.Build();

			List<Embed> embeds = new List<Embed>() { e };

			ulong msgID = await webhookClient.SendMessageAsync(null, false, embeds);

			return (msgID == 0) ? null : msgID;
		}

		private Task ConsoleLog(LogMessage message)
		{
			Console.WriteLine(message.ToString());

			return Task.CompletedTask;
		}
	}
}
