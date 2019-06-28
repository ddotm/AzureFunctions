using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payload;
using System.Threading.Tasks;
using Utility;

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
                .AddJsonFile("app.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var config = configBuilder.Build();

            Config.SetConfig(config);
            Logger.SetLogger(log);

			var payload = new FunctionalPayload();
            await payload.ExecuteAsync();
        }
    }
}