using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Lumpy.Lib.Common
{
    public static class LoggerInstance
    {
        public static ILogger ConsoleLogger() => ConsoleLoggerConfiguration().CreateLogger();

        public static ILogger FileLogger(
            string logFilePath
            , LogEventLevel level = LogEventLevel.Verbose
            , string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{Caller}] {Message}{NewLine}{Exception}"
            , int retainedFileCountLimit = 10
            ) 
            => FileLoggerConfiguration(logFilePath,level,template).CreateLogger();

        public static ILogger ConfiguredLogger(IConfiguration loggerConfiguration)
        {
            return loggerConfiguration == null
                ? throw new ArgumentNullException(nameof(loggerConfiguration))
                : new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(loggerConfiguration)
                    .CreateLogger();
        }


        public static LoggerConfiguration ConsoleFileLoggerConfiguration(
            string logFilePath,
            LogEventLevel consoleLogLevel = LogEventLevel.Verbose,
            LogEventLevel fileLogLevel = LogEventLevel.Verbose
        ) =>
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithCaller()
                .Enrich.WithThreadId()
                .WriteTo
                .Async(a => 
                    a.Console(
                        consoleLogLevel,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} {Message}{NewLine}{Exception}"))
                .WriteTo
                .Async(a =>
                {
                    a.File(
                        logFilePath,
                        fileLogLevel,
                        rollingInterval: RollingInterval.Day,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{Caller}] {Message}{NewLine}{Exception}");
                });

        public static LoggerConfiguration ConsoleLoggerConfiguration() =>
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithCaller()
                .Enrich.WithThreadId().WriteTo.Async(a => a.Console(
                    LogEventLevel.Verbose,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{Caller}] {Message}{NewLine}{Exception}"));

        public static LoggerConfiguration FileLoggerConfiguration(
            string logFilePath
            , LogEventLevel level = LogEventLevel.Verbose
            , string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{Caller}] {Message}{NewLine}{Exception}"
            , int retainedFileCountLimit = 10
            ) 
            => string.IsNullOrEmpty(logFilePath) || string.IsNullOrWhiteSpace(logFilePath)
                ? throw new ArgumentNullException(nameof(logFilePath))
                : new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.FromLogContext()
                    .Enrich.WithCaller()
                    .Enrich.WithThreadId()
                    .WriteTo
                    .Async(a =>
                    {
                        a.File(
                            logFilePath,
                            level,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit:retainedFileCountLimit,
                            outputTemplate: template);
                    });
    }
}
