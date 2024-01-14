using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GPGBot.ContinuousIntegration
{
	public class JenkinsCI : CIBase, IContinuousIntegrationSystem
	{
		public JenkinsCI(Config.ContinuousIntegration config) : base(config)
		{
		}

		public async Task<bool> StartBuild(string jobName)
		{
			HttpClient client = new();

			FormUrlEncodedContent requestContent = new(new[] { new KeyValuePair<string, string>("text", "text"), });

			HttpResponseMessage response = await client.PostAsync(address, requestContent);

			HttpContent responseContent = response.Content;

			using (StreamReader reader = new(await responseContent.ReadAsStreamAsync()))
			{
				Console.WriteLine(await reader.ReadToEndAsync());
			}

			return false;
		}
	}
}
