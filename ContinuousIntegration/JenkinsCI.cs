using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using PercivalBot.ContinuousIntegration.Interface;

namespace PercivalBot.ContinuousIntegration
{
    public class JenkinsCI : BaseCI, IContinuousIntegrationSystem
	{
		public JenkinsCI(Config.ContinuousIntegrationConfig config) : base(config)
		{
		}

		public async Task<bool> StartJob(string jobName, bool buildCode, bool buildWwise)
		{
			await Task.Delay(1); // shut up warnings
			throw new NotImplementedException();

			/*
			HttpClient client = new();

			FormUrlEncodedContent requestContent = new(new[] { new KeyValuePair<string, string>("text", "text"), });

			HttpResponseMessage response = await client.PostAsync(address, requestContent);

			HttpContent responseContent = response.Content;

			using (StreamReader reader = new(await responseContent.ReadAsStreamAsync()))
			{
				Console.WriteLine(await reader.ReadToEndAsync());
			}

			return false;
			*/
		}

		public Task<bool> StartJob(string jobName, string changeID, bool buildCode, bool buildWwise)
		{
			throw new NotImplementedException();
		}
	}
}
