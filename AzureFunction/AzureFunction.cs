using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AzureFunction
{
	public static class AzureFunction
	{
		[FunctionName("AzureFunction")]
		public static void Run([TimerTrigger("* */35 * * * *")] TimerInfo myTimer, ILogger log)
		{
			log.LogInformation($"Azure Function triggered by timer. Executed at: {DateTime.Now}");
		}
	}
}