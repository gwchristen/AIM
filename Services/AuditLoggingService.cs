using AIM.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Audit logging service that uses Serilog to write structured audit logs.
/// Logs are written to %LOCALAPPDATA%\AIM\Logs\audit.log with daily rolling.
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
    private readonly ILogger _auditLogger;
    private readonly string _logFilePath;

    /// <summary>
    /// Initializes a new instance of the AuditLoggingService.
    /// The logger is configured separately in App.xaml.cs.
    /// </summary>
    public AuditLoggingService()
    {
        // Get the logger instance configured in App.xaml.cs
        _auditLogger = Log.Logger;
        
        // Store the log file path for reading
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDirectory = Path.Combine(localAppData, "AIM", "Logs");
        _logFilePath = Path.Combine(logDirectory, "audit.log");
    }

    /// <summary>
    /// Logs an audit event with structured data.
    /// </summary>
    public void LogAudit(string actionType, string? targetPath = null, string? description = null, Dictionary<string, string>? additionalData = null)
    {
        var timestamp = DateTime.UtcNow;
        var userName = Environment.UserName;

        // Create structured log entry
        var logData = new Dictionary<string, object>
        {
            { "Timestamp", timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
            { "User", userName },
            { "ActionType", actionType },
            { "TargetPath", targetPath ?? string.Empty },
            { "Description", description ?? string.Empty }
        };

        // Add additional data if provided
        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                logData[$"Data_{kvp.Key}"] = kvp.Value;
            }
        }

        // Log as structured data with Serilog
        _auditLogger.Information(
            "AUDIT | {Timestamp} | {User} | {ActionType} | {TargetPath} | {Description} | {AdditionalData}",
            timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            userName,
            actionType,
            targetPath ?? string.Empty,
            description ?? string.Empty,
            additionalData != null ? JsonSerializer.Serialize(additionalData) : string.Empty
        );
    }

    /// <summary>
    /// Asynchronously reads audit log entries from the log file.
    /// Parses the Serilog text format and returns structured LogEntry objects.
    /// </summary>
    public async Task<IEnumerable<LogEntry>> ReadAuditLogsAsync(int maxEntries = 1000)
    {
        var logEntries = new List<LogEntry>();

        if (!File.Exists(_logFilePath))
        {
            return logEntries;
        }

        try
        {
            // Read all lines from the log file
            var lines = await File.ReadAllLinesAsync(_logFilePath);
            
            // Take the last maxEntries lines (most recent)
            var recentLines = lines.Reverse().Take(maxEntries).Reverse();

            foreach (var line in recentLines)
            {
                try
                {
                    // Parse Serilog format: [Timestamp] [Level] Message
                    // Expected format: [yyyy-MM-dd HH:mm:ss] [Information] AUDIT | timestamp | user | action | path | description | data
                    
                    if (!line.Contains("AUDIT |"))
                        continue;

                    var auditPart = line.Substring(line.IndexOf("AUDIT |") + 8);
                    var parts = auditPart.Split('|').Select(p => p.Trim()).ToArray();

                    if (parts.Length >= 5)
                    {
                        var entry = new LogEntry
                        {
                            Timestamp = DateTime.TryParse(parts[0], out var ts) ? ts : DateTime.MinValue,
                            UserName = parts[1],
                            Action = parts[2],
                            Details = $"Path: {parts[3]}, Description: {parts[4]}"
                        };

                        // Add additional data if present
                        if (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                        {
                            entry.Details += $", Data: {parts[5]}";
                        }

                        logEntries.Add(entry);
                    }
                }
                catch
                {
                    // Skip malformed log lines
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuditLoggingService] Error reading audit logs: {ex.Message}");
        }

        return logEntries;
    }
}
