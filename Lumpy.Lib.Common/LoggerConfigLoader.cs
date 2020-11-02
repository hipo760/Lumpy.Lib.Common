using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Lumpy.Lib.Common
{
    public class LoggerConfigLoader
    {
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }

        public LoggerConfigLoader(string configFile = null)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                Logger = BuildConsoleLogger();
                return;
            }
            else
            {
                try
                {
                    Configuration = LoadConfiguration(configFile);
                    Logger = Configuration != null ? BuildLogger(Configuration) : null;
                }
                catch (System.Exception)
                {
                    throw;
                }
            }
        }
        private static IConfiguration LoadConfiguration(string configFile)
            => new ConfigurationBuilder()
            .AddJsonFile(configFile, optional: true)
            .Build();
        private static ILogger BuildLogger(IConfiguration configuration) 
            => new LoggerConfiguration()
                .ReadFrom
                .Configuration(configuration)
                .CreateLogger();

        private static ILogger BuildConsoleLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithCaller()
                .Enrich.WithThreadId().WriteTo.Async(a => a.Console(
                    LogEventLevel.Verbose,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) {Caller} {Message}{NewLine}{Exception}"))
                .CreateLogger();
        }
    }
}
