using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AzureFunction
{
	public static class AzureFunction
	{
		[FunctionName("AzureFunction")]
		public static async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer, ILogger log)
		{
			log.LogInformation($"Azure Function triggered by timer. Executed at: {DateTime.Now}");
			await Task.CompletedTask;
		}
	}
}