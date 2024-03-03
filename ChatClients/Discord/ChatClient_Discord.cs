using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using PercivalBot.ChatClients.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

using PercivalBot.Enums;
using PercivalBot.Structs;
using PercivalBot.Config;

namespace PercivalBot.ChatClients.Discord
{
    public class ChatClient_Discord : IChatClient
	{
		readonly Config.ChatClientConfig config;

		readonly DiscordSocketClient client;

		readonly IEmbedBuilder embedBuilder;

		readonly ulong guildID;

		bool clientReady = false;

		CommitStyle commitStyle;

		protected Dictionary<EBuildStatus, EmbedStyleConfig> buildEmbedStyles = new()
		{
			{ EBuildStatus.Running, new() },
			{ EBuildStatus.Succeeded, new() },
			{ EBuildStatus.Failed, new() },
			{ EBuildStatus.Unstable, new() },
			{ EBuildStatus.Aborted, new() },
		};

		public ChatClient_Discord(ChatClientConfig inConfig, IEmbedBuilder inEmbedBuilder)
		{
			config = inConfig;
			embedBuilder = inEmbedBuilder;

			if (!ulong.TryParse(inConfig.ServerID, out guildID))
			{
				guildID = 0;
			}

			DiscordSocketConfig clientConfig = new DiscordSocketConfig();
			client = new DiscordSocketClient(clientConfig);

			commitStyle = inConfig.CommitStyle ?? new();

			if (inConfig.BuildEmbedStyle != null)
			{
				foreach (var style in inConfig.BuildEmbedStyle)
				{
					if (style.Name == null)
					{
						Console.WriteLine($"Config error: embed style with null name");
						continue;
					}

					EBuildStatus status;

					if (Enum.TryParse(style.Name, true, out status))
					{
						buildEmbedStyles[status] = style;
					}
				}
			}
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

		public async Task<ulong?> PostBuildStatusEmbed(BuildStatusEmbedData embedData, string channelName)
		{
			IChannel? channel = FindChannel(channelName);

			if (channel == null)
			{
				Console.WriteLine($"Warning: channel <{channelName}> not found!");
				return null;
			}

			if (channel is IMessageChannel messageChannel)
			{
				Embed e = embedBuilder.ConstructBuildStatusEmbed(embedData);
				IUserMessage sentMessage = await messageChannel.SendMessageAsync(null, false, e);
				return sentMessage.Id;
			}

			Console.WriteLine($"Channel <{channelName}> was found, but was not a message channel!");
			return null;
		}

		public async Task DeleteMessage(ulong messageID, string channelName)
		{
			IChannel? channel = FindChannel(channelName);

			if (channel is IMessageChannel messageChannel)
			{
				RequestOptions options = new RequestOptions();
				await messageChannel.DeleteMessageAsync(messageID);
			}
			else
			{
				throw new Exception($"Unable to delete message {messageID} from channel {channelName} (channel not found)");
			}

			await Task.CompletedTask;
		}

		public IChannel? FindChannel(string channelName)
		{
			SocketGuild guild = client.GetGuild(guildID);

			IChannel? channel = null;

			foreach (SocketGuildChannel? channelCandidate in guild.Channels)
			{
				if (channelCandidate.Name == channelName)
				{
					channel = channelCandidate;
				}
			}

			return channel;
		}

		public async Task<ulong?> PostCommitMessage(CommitEmbedData embedData, string commitWebhook)
		{
			DiscordWebhookClient webhookClient = new(commitWebhook);

			webhookClient.Log += ConsoleLog;

			string titleText = string.Format("Change {0} \u2022 {1}", embedData.change, embedData.client);

			// TODO remove hardcoding
			string changeIcon = commitStyle.IconUrl;
			Color color = new(uint.Parse(embedData.change));
			EmbedBuilder builder = new EmbedBuilder()
				.WithAuthor(titleText, changeIcon)
				.WithDescription(embedData.description)
				.WithColor(1094111);

			Embed e = builder.Build();

			List<Embed> embeds = new List<Embed>() { e };

			ulong msgID = await webhookClient.SendMessageAsync(null, false, embeds);

			return msgID == 0 ? null : msgID;
		}

		private Task ConsoleLog(LogMessage message)
		{
			Console.WriteLine(message.ToString());

			return Task.CompletedTask;
		}
	}
}
