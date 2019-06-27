using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payload;
using System.Threading.Tasks;

namespace AzureFunction
{
	public static class AzureFunction
	{
		[FunctionName("AzureFunction")]
		public static async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer, ILogger log,
			ExecutionContext context)
		{
			var configBuilder = new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile("app.settings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables();
			var config = configBuilder.Build();

			var payload = new FunctionalPayload(config, log);
			await payload.ExecuteAsync();
		}
	}
}