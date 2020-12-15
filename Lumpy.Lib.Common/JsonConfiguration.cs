using System;
using Microsoft.Extensions.Configuration;

namespace Lumpy.Lib.Common
{
    public static class JsonConfiguration
    {
        public static IConfiguration LoadConfigurationFromFile(string configFile)
        {
            return string.IsNullOrEmpty(configFile) || string.IsNullOrWhiteSpace(configFile)
                ? throw new ArgumentNullException(nameof(configFile))
                : new ConfigurationBuilder().AddJsonFile(configFile, optional: true).Build();
        }
        public static TOptions Configure<TOptions>(IConfiguration config) where TOptions : class, new()
        {
            var options = new TOptions();
            config.Bind(options);
            return options;
        }
    }
}