using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Payload
{
	public class FunctionalPayload
	{
		private readonly IConfigurationRoot _config;
		private readonly ILogger _log;

		public FunctionalPayload(IConfigurationRoot config, ILogger log)
		{
			_config = config;
			_log = log;
		}

		public async Task ExecuteAsync()
		{
			_log.LogInformation($"Azure Function with timer trigger (with CI/CD). Executed at: {DateTime.Now}");
			_log.LogInformation($"Configuration data SQLConnectStr1: {_config["SQLConnectStr1"]}");

			await Task.CompletedTask;
		}
	}
}