using Perforce.P4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;

namespace GPGBot.ContinuousIntegration
{
	public class TeamCityCI : CIBase, IContinuousIntegrationSystem
	{
		public TeamCityCI(Config.ContinuousIntegration config) : base(config)
		{
		}

		public async Task<bool> StartBuild(string jobName)
		{
			Uri uri = new Uri(new Uri(address), "app/rest/buildQueue");

			HttpClient client = new();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
			
			string xmlContent = "<build><buildType id='" + jobName + "'/></build>";
			StringContent requestContent = new(xmlContent, System.Text.Encoding.UTF8, "application/xml");

			HttpResponseMessage response = await client.PostAsync(uri, requestContent);

			if (!response.IsSuccessStatusCode)
			{
				return false;
			}
			else
			{
				return true;
			}
		}
	}
}
