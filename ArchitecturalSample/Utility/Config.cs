using Microsoft.Extensions.Configuration;

namespace Utility
{
    public static class Config
    {
        private static IConfigurationRoot _config;
        public static string ConfigProperty1 { get; private set; }
        public static string ConfigProperty2 { get; private set; }


        public static void SetConfig(IConfigurationRoot config)
        {
            _config = config;
            ConfigProperty1 = config["ConfigProperty1"];
            ConfigProperty2 = config["ConfigProperty2"];
        }

        public static void DumpConfig()
        {
            Logger.Write($"ConfigProperty1: {ConfigProperty1}");
            Logger.Write($"ConfigProperty2: {ConfigProperty2}");
        }
    }
}
