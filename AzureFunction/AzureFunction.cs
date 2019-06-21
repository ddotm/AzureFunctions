using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Payload;

namespace AzureFunction
{
	public static class AzureFunction
	{
		[FunctionName("AzureFunction")]
		public static async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer, ILogger log)
		{
			var payload = new FunctionalPayload(log);
			await payload.ExecuteAsync();
		}
	}
}