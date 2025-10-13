using AIM.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

public class LoggingService : ILoggingService
{
    public LoggingService()
    {
        // Configure Serilog globally if not already done
        if (Log.Logger == null)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/aim.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }

    public async Task LogAsync(string action, string details = "")
    {
        string message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {Environment.UserName} | {action} | {details}";
        Log.Information(message);
    }

    public async Task<IEnumerable<LogEntry>> GetLogsAsync()
    {
        // Implement if needed
        return new List<LogEntry>();
    }
}