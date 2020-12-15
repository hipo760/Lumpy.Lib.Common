using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Lumpy.Lib.Common
{
    public static class LoggerInstance
    {
        public static ILogger ConsoleLogger() => BuildConsoleLogger();
        public static ILogger FileLogger(string logFilePath) => BuildFileLogger(logFilePath);
        public static ILogger ConfiguredLogger(IConfiguration loggerConfiguration)
        {
            return loggerConfiguration == null
                ? throw new ArgumentNullException(nameof(loggerConfiguration))
                : new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(loggerConfiguration)
                    .CreateLogger();
        }
        private static ILogger BuildConsoleLogger() =>
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithCaller()
                .Enrich.WithThreadId().WriteTo.Async(a => a.Console(
                    LogEventLevel.Verbose,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{Caller}] {Message}{NewLine}{Exception}"))
                .CreateLogger();
        private static ILogger BuildFileLogger(string logFilePath) =>
            string.IsNullOrEmpty(logFilePath) || string.IsNullOrWhiteSpace(logFilePath)
                ? throw new ArgumentNullException(nameof(logFilePath))
                : new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.FromLogContext()
                    .Enrich.WithCaller()
                    .Enrich.WithThreadId().WriteTo.Async(a =>
                    {
                        a.File(
                            logFilePath,
                            LogEventLevel.Verbose,
                            rollingInterval: RollingInterval.Day,
                            outputTemplate:
                            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{Caller}] {Message}{NewLine}{Exception}");
                    })
                    .CreateLogger();
    }
}
