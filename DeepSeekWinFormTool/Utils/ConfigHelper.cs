using Microsoft.Extensions.Configuration;
using System.IO;

namespace DeepSeekBatchTool.Utils
{
    public static class ConfigHelper
    {
        private static IConfigurationRoot _config;

        static ConfigHelper()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _config = builder.Build();
        }

        public static string ApiKey => _config["DeepSeek:ApiKey"];
        public static string ModelName => _config["DeepSeek:ModelName"];
        public static int MaxConcurrent => int.Parse(_config["DeepSeek:MaxConcurrent"] ?? "3");
        public static string DefaultSystemPrompt => _config["DeepSeek:DefaultSystemPrompt"];
        public static string CacheFilePath => _config["Cache:CacheFilePath"] ?? "ai_cache.json";
        public static string LicenseSalt => _config["License:Salt"] ?? "DefaultSalt";
    }
}