using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Utility
{
    public static class Logger
    {
        public static StringBuilder StrBuilder = new StringBuilder("");
        private static ILogger _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public static void Write(string message)
        {
            _logger.LogInformation(message);
            AppendMessage(message);
        }

        public static void WriteError(Exception e)
        {
            _logger.LogError(e.Message);
            AppendMessage(e.Message);
        }

        private static void AppendMessage(string message)
        {
            StrBuilder.Append($"[{DateTime.UtcNow:yyyy/MM/dd hh:mm:ss}]   {message}");
        }
    }
}
