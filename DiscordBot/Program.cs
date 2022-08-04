using System;
using System.IO;
using Discord;
using RustyWatcher.Configurations;
using RustyWatcher.Controllers;
using RustyWatcher.Helpers;
using RustyWatcher.Workers;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace RustyWatcher
{
    public class Program
    {
        private const string LOG_FOLDER = "Logs";
        private const string LOG_FILE = "log.txt";

        public static readonly RequestOptions RetryAlwaysRequest = new()
            {RetryMode = RetryMode.AlwaysRetry, Timeout = 5};
        
        private static LogConfiguration _logConfiguration => Configuration.Instance.LogConfiguration;
        
        private static void Main()
        {
            Configuration.Load();
            
            // Setup Serilog
            var logConfiguration = new LoggerConfiguration()
                .Enrich.FromLogContext().MinimumLevel.Verbose();
            
            logConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code, restrictedToMinimumLevel: _logConfiguration.LevelLogging);

            if (_logConfiguration.FileLogging)
                logConfiguration.WriteTo.File(Path.Combine(Path.Combine(Utilities.GetBasePath(), LOG_FOLDER), LOG_FILE),
                    rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: _logConfiguration.LevelLogging);
            
            Log.Logger = logConfiguration.CreateLogger();
            
            var startupString = $"RustyWatcher {Utilities.GetVersionString()} made by collect_vood#3773";
            Console.Title = startupString;
            Log.Information(startupString);
            
            Log.Information("Log Level: {0}", _logConfiguration.LevelLogging);
        
            Connector.StartAll();
            InfluxWorker.Start();
            
            Console.ReadKey();
        }
    }
}
