using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Payload
{
	public class FunctionalPayload
	{
		private readonly ILogger _log;

		public FunctionalPayload(ILogger log)
		{
			_log = log;
		}

		public async Task ExecuteAsync()
		{
			_log.LogInformation($"Azure Function with timer trigger (with CI/CD). Executed at: {DateTime.Now}");

			await Task.CompletedTask;
		}
	}
}