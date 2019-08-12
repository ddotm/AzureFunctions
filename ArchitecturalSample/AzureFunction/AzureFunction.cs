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
        [FunctionName("AzureFunction1")]
        public static async Task Func1RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer, ILogger log,
            ExecutionContext context)
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                //.AddJsonFile("app.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var config = configBuilder.Build();

            Config.SetConfig(config);
            Logger.SetLogger(log);

            Logger.Write("Function 1 running...");
			var payload = new FunctionalPayload();
            await payload.ExecuteAsync();
        }

        [FunctionName("AzureFunction2")]
        public static async Task Func2RunAsync([TimerTrigger("*/7 * * * * *")] TimerInfo myTimer, ILogger log,
	        ExecutionContext context)
        {
	        var configBuilder = new ConfigurationBuilder()
		        .SetBasePath(context.FunctionAppDirectory)
		        //.AddJsonFile("app.settings.json", optional: true, reloadOnChange: true)
		        .AddEnvironmentVariables();
	        var config = configBuilder.Build();

	        Config.SetConfig(config);
	        Logger.SetLogger(log);

			Logger.Write("Function 2 running...");
	        var payload = new FunctionalPayload();
	        await payload.ExecuteAsync();
        }
	}
}