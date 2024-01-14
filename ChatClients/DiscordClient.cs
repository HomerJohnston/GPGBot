using Discord;
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
		Config.ChatClient config;

		DiscordSocketClient client;

		IEmbedBuilder embedBuilder;

		ulong defaultChannelID;

		public DiscordClient(Config.ChatClient inConfig, IEmbedBuilder inEmbedBuilder)
		{
			config = inConfig;
			embedBuilder = inEmbedBuilder;
			defaultChannelID = inConfig.defaultChannelID ?? 0;

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

			Embed e = embedBuilder.BuildEmbed(embedData);

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
	}
}
