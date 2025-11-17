using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Audit logging service that records all user actions with timestamps, user ID, and details.
/// Logs are stored as JSON in a local file for easy searching and review.
/// </summary>
public class AuditLoggingService
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private const string LOG_FILENAME = "audit_log.json";

    public AuditLoggingService()
    {
        // Store logs in AppData\Local\AIM\Logs
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIM",
            "Logs"
        );

        _logFilePath = Path.Combine(_logDirectory, LOG_FILENAME);

        // Create directory if it doesn't exist
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
            Debug.WriteLine($"[AuditLogging] Created log directory: {_logDirectory}");
        }
    }

    /// <summary>
    /// Log a file operation with detailed before/after information.
    /// </summary>
    public void LogFileOperation(string actionType, string filePath, string description, Dictionary<string, string> details = null)
    {
        var entry = new AuditLogEntry
        {
            ActionType = actionType,
            Description = description,
            TargetPath = filePath,
            UserId = Environment.UserName,
            Details = details != null ? JsonSerializer.Serialize(details) : ""
        };

        LogAction(entry);
    }

    /// <summary>
    /// Log a move operation (from -> to).
    /// </summary>
    public void LogMoveOperation(string sourceFile, string destinationFile, string fileName)
    {
        var details = new Dictionary<string, string>
        {
            { "operation", "move" },
            { "from", sourceFile },
            { "to", destinationFile },
            { "fileName", fileName }
        };

        LogFileOperation(
            AuditActionTypes.FILE_MOVE,
            sourceFile,
            $"Moved file '{fileName}' from {Path.GetDirectoryName(sourceFile)} to {Path.GetDirectoryName(destinationFile)}",
            details
        );
    }

    /// <summary>
    /// Log a copy operation (from -> to).
    /// </summary>
    public void LogCopyOperation(string sourceFile, string destinationFile, string fileName)
    {
        var details = new Dictionary<string, string>
        {
            { "operation", "copy" },
            { "from", sourceFile },
            { "to", destinationFile },
            { "fileName", fileName }
        };

        LogFileOperation(
            AuditActionTypes.FILE_COPY,
            sourceFile,
            $"Copied file '{fileName}' from {Path.GetDirectoryName(sourceFile)} to {Path.GetDirectoryName(destinationFile)}",
            details
        );
    }

    /// <summary>
    /// Log a rename operation (old name -> new name).
    /// </summary>
    public void LogRenameOperation(string filePath, string oldName, string newName)
    {
        var details = new Dictionary<string, string>
        {
            { "operation", "rename" },
            { "oldName", oldName },
            { "newName", newName },
            { "directory", Path.GetDirectoryName(filePath) }
        };

        LogFileOperation(
            AuditActionTypes.FILE_RENAME,
            filePath,
            $"Renamed '{oldName}' to '{newName}'",
            details
        );
    }

    /// <summary>
    /// Log a delete operation.
    /// </summary>
    public void LogDeleteOperation(string filePath, string fileName, bool isDirectory = false)
    {
        var details = new Dictionary<string, string>
        {
            { "operation", "delete" },
            { "fileName", fileName },
            { "isDirectory", isDirectory.ToString() },
            { "directory", Path.GetDirectoryName(filePath) }
        };

        LogFileOperation(
            isDirectory ? AuditActionTypes.DIR_DELETE : AuditActionTypes.FILE_DELETE,
            filePath,
            $"Deleted {(isDirectory ? "directory" : "file")} '{fileName}'",
            details
        );
    }

    /// <summary>
    /// Log a file access/open operation.
    /// </summary>
    public void LogFileAccess(string filePath, string fileName, string accessType = "opened")
    {
        var details = new Dictionary<string, string>
        {
            { "operation", "access" },
            { "fileName", fileName },
            { "accessType", accessType },
            { "directory", Path.GetDirectoryName(filePath) }
        };

        LogFileOperation(
            AuditActionTypes.FILE_OPEN,
            filePath,
            $"File '{fileName}' was {accessType}",
            details
        );
    }

    /// <summary>
    /// Log a preview operation with file details.
    /// </summary>
    public void LogPreviewOperation(string filePath, string fileName, string fileSize = "", string fileType = "")
    {
        var details = new Dictionary<string, string>
        {
            { "operation", "preview" },
            { "fileName", fileName },
            { "fileSize", fileSize },
            { "fileType", fileType },
            { "directory", Path.GetDirectoryName(filePath) }
        };

        LogFileOperation(
            AuditActionTypes.FILE_PREVIEW,
            filePath,
            $"Previewed file '{fileName}'",
            details
        );
    }

    /// <summary>
    /// Log a directory operation.
    /// </summary>
    public void LogDirectoryOperation(string actionType, string directoryPath, string description, Dictionary<string, string> details = null)
    {
        var entry = new AuditLogEntry
        {
            ActionType = actionType,
            Description = description,
            TargetPath = directoryPath,
            UserId = Environment.UserName,
            Details = details != null ? JsonSerializer.Serialize(details) : ""
        };

        LogAction(entry);
    }

    /// <summary>
    /// Log an action with all relevant details.
    /// </summary>
    public async Task LogActionAsync(AuditLogEntry entry)
    {
        try
        {
            // Set timestamp and current user if not already set
            entry.Timestamp = DateTime.UtcNow;
            if (string.IsNullOrEmpty(entry.UserId))
            {
                entry.UserId = Environment.UserName;
            }

            // Load existing logs
            var logs = await LoadLogsAsync();
            logs.Add(entry);

            // Save updated logs
            await SaveLogsAsync(logs);

            Debug.WriteLine($"[AuditLogging] Action logged: {entry.ActionType} - {entry.Description}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR logging action: {ex.Message}");
        }
    }

    /// <summary>
    /// Log an action synchronously (for convenience in non-async contexts).
    /// </summary>
    public void LogAction(AuditLogEntry entry)
    {
        try
        {
            // Set timestamp and current user if not already set
            entry.Timestamp = DateTime.UtcNow;
            if (string.IsNullOrEmpty(entry.UserId))
            {
                entry.UserId = Environment.UserName;
            }

            // Load existing logs
            var logs = LoadLogsSynchronous();
            logs.Add(entry);

            // Save updated logs
            SaveLogsSynchronous(logs);

            Debug.WriteLine($"[AuditLogging] Action logged: {entry.ActionType} - {entry.Description}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR logging action: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieve all audit logs.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetLogsAsync()
    {
        return await LoadLogsAsync();
    }

    /// <summary>
    /// Retrieve logs filtered by action type.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetLogsByActionAsync(string actionType)
    {
        var logs = await LoadLogsAsync();
        return logs.FindAll(l => l.ActionType.Equals(actionType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retrieve logs for a specific user.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetLogsByUserAsync(string userId)
    {
        var logs = await LoadLogsAsync();
        return logs.FindAll(l => l.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retrieve logs within a date range.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var logs = await LoadLogsAsync();
        return logs.FindAll(l => l.Timestamp >= startDate && l.Timestamp <= endDate);
    }

    /// <summary>
    /// Clear all logs - RESTRICTED: Only callable by authorized users
    /// </summary>
    public async Task ClearLogsAsync(bool isAuthorized = false)
    {
        if (!isAuthorized)
        {
            Debug.WriteLine($"[AuditLogging] DENIED: Unauthorized attempt to clear logs");
            throw new UnauthorizedAccessException("You do not have permission to clear audit logs");
        }

        try
        {
            if (File.Exists(_logFilePath))
            {
                File.Delete(_logFilePath);
                Debug.WriteLine($"[AuditLogging] Audit logs cleared by authorized user");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR clearing logs: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Log a clear logs attempt (success or failure)
    /// </summary>
    public void LogClearLogsAttempt(bool success, string userId)
    {
        var entry = new AuditLogEntry
        {
            ActionType = success ? "LOGS_CLEARED" : "LOGS_CLEAR_DENIED",
            Description = success ? "Audit logs were cleared" : "Unauthorized attempt to clear audit logs",
            TargetPath = "AUDIT_SYSTEM",
            UserId = userId,
            Details = ""
        };

        LogAction(entry);
    }

    /// <summary>
    /// Export logs to a CSV file.
    /// </summary>
    public async Task ExportToCSVAsync(string exportPath)
    {
        try
        {
            var logs = await LoadLogsAsync();

            using (var writer = new StreamWriter(exportPath))
            {
                // Write header
                await writer.WriteLineAsync("Timestamp,UserId,ActionType,Description,TargetPath,Details");

                // Write data
                foreach (var log in logs)
                {
                    var details = log.Details?.Replace(",", ";").Replace("\n", " ") ?? "";
                    await writer.WriteLineAsync(
                        $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.UserId}\",\"{log.ActionType}\",\"{log.Description}\",\"{log.TargetPath}\",\"{details}\""
                    );
                }
            }

            Debug.WriteLine($"[AuditLogging] Logs exported to: {exportPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR exporting logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Export logs to a JSON file.
    /// </summary>
    public async Task ExportToJsonAsync(string exportPath, List<AuditLogEntry> logsToExport)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(logsToExport, options);
            await File.WriteAllTextAsync(exportPath, json);

            Debug.WriteLine($"[AuditLogging] Logs exported to JSON: {exportPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR exporting logs to JSON: {ex.Message}");
        }
    }

    private async Task<List<AuditLogEntry>> LoadLogsAsync()
    {
        if (!File.Exists(_logFilePath))
        {
            return new List<AuditLogEntry>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_logFilePath);
            var logs = JsonSerializer.Deserialize<List<AuditLogEntry>>(json) ?? new List<AuditLogEntry>();
            return logs;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR loading logs: {ex.Message}");
            return new List<AuditLogEntry>();
        }
    }

    private List<AuditLogEntry> LoadLogsSynchronous()
    {
        if (!File.Exists(_logFilePath))
        {
            return new List<AuditLogEntry>();
        }

        try
        {
            var json = File.ReadAllText(_logFilePath);
            var logs = JsonSerializer.Deserialize<List<AuditLogEntry>>(json) ?? new List<AuditLogEntry>();
            return logs;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR loading logs: {ex.Message}");
            return new List<AuditLogEntry>();
        }
    }

    private async Task SaveLogsAsync(List<AuditLogEntry> logs)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(logs, options);
            await File.WriteAllTextAsync(_logFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR saving logs: {ex.Message}");
        }
    }

    private void SaveLogsSynchronous(List<AuditLogEntry> logs)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(logs, options);
            File.WriteAllText(_logFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuditLogging] ERROR saving logs: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a single audit log entry containing information about a user action.
/// Entries are serialized to JSON for persistent storage and review.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the action occurred (UTC).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user ID (Windows username) who performed the action.
    /// Defaults to the current Windows user if not explicitly set.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = Environment.UserName;

    /// <summary>
    /// Gets or sets the type of action performed (e.g., "FILE_ACCESS", "FILE_DELETE", "FILE_MOVE").
    /// See <see cref="AuditActionTypes"/> for standard action type constants.
    /// </summary>
    [JsonPropertyName("actionType")]
    public string ActionType { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the action.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the file or folder path affected by the action.
    /// </summary>
    [JsonPropertyName("targetPath")]
    public string TargetPath { get; set; }

    /// <summary>
    /// Gets or sets additional details about the action in JSON format.
    /// Contains operation-specific information such as source/destination paths, file names, etc.
    /// </summary>
    [JsonPropertyName("details")]
    public string Details { get; set; }
}

/// <summary>
/// Provides standard action type constants for audit logging.
/// Use these constants to ensure consistency in audit log entries.
/// </summary>
public static class AuditActionTypes
{
    // File Operations
    /// <summary>File was accessed or opened for reading.</summary>
    public const string FILE_ACCESS = "FILE_ACCESS";
    
    /// <summary>File was previewed in the application.</summary>
    public const string FILE_PREVIEW = "FILE_PREVIEW";
    
    /// <summary>File was opened with an external application.</summary>
    public const string FILE_OPEN = "FILE_OPEN";
    
    /// <summary>File was copied to another location.</summary>
    public const string FILE_COPY = "FILE_COPY";
    
    /// <summary>File was moved to another location.</summary>
    public const string FILE_MOVE = "FILE_MOVE";
    
    /// <summary>File was deleted.</summary>
    public const string FILE_DELETE = "FILE_DELETE";
    
    /// <summary>File was renamed.</summary>
    public const string FILE_RENAME = "FILE_RENAME";
    
    /// <summary>File was created.</summary>
    public const string FILE_CREATE = "FILE_CREATE";

    // Directory Operations
    /// <summary>Directory was accessed or browsed.</summary>
    public const string DIR_ACCESS = "DIR_ACCESS";
    
    /// <summary>Directory was created.</summary>
    public const string DIR_CREATE = "DIR_CREATE";
    
    /// <summary>Directory was deleted.</summary>
    public const string DIR_DELETE = "DIR_DELETE";
    
    /// <summary>Directory was renamed.</summary>
    public const string DIR_RENAME = "DIR_RENAME";

    // Security Actions
    /// <summary>Master password was successfully validated and override activated.</summary>
    public const string MASTER_UNLOCK = "MASTER_UNLOCK";
    
    /// <summary>Master password override was deactivated.</summary>
    public const string MASTER_LOCK = "MASTER_LOCK";
    
    /// <summary>Master password was changed.</summary>
    public const string MASTER_PASSWORD_CHANGED = "MASTER_PASSWORD_CHANGED";
    
    /// <summary>User was added to the authorized users list.</summary>
    public const string USER_ADDED = "USER_ADDED";
    
    /// <summary>User was removed from the authorized users list.</summary>
    public const string USER_REMOVED = "USER_REMOVED";
    
    /// <summary>Application settings were modified.</summary>
    public const string SETTINGS_CHANGED = "SETTINGS_CHANGED";

    // Search/Preview
    /// <summary>Search operation was performed.</summary>
    public const string SEARCH_PERFORMED = "SEARCH_PERFORMED";
    
    /// <summary>Filter was applied to data view.</summary>
    public const string FILTER_APPLIED = "FILTER_APPLIED";
}