using System;
using System.Threading.Tasks;
using Utility;

namespace Payload
{
	public class FunctionalPayload
	{
		public async Task ExecuteAsync()
		{
			try
			{
				Config.DumpConfig();
				await Task.CompletedTask;
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				throw;
			}
		}
	}
}