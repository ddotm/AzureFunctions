using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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