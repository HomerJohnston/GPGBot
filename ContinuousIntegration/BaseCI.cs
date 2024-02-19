using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.ContinuousIntegration
{
	public abstract class BaseCI
	{
		protected readonly string address;
		protected readonly string user;
		protected readonly string password;
		protected readonly string token;

		public BaseCI(Config.ContinuousIntegration config)
		{
			if (config.Address == null)
			{
				throw new ArgumentException("Continuous Integration address not set!");
			}


			bool result = Uri.TryCreate(config.Address, UriKind.Absolute, out Uri? uriResult)
				&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

			if (!result)
			{
				throw new ArgumentException("Continuous Integration address malformed!");
			}

			address = config.Address;
			user = config.User ?? string.Empty;
			password = config.Password ?? string.Empty;
			token = config.Token ?? string.Empty;
		}
	}
}
