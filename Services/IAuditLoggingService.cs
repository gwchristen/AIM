using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Interface for audit logging service that provides structured logging for compliance and tracking.
/// </summary>
public interface IAuditLoggingService
{
    /// <summary>
    /// Logs an audit event with structured data.
    /// </summary>
    /// <param name="actionType">The type of action being performed (e.g., "FILE_MOVE", "SETTINGS_CHANGED").</param>
    /// <param name="targetPath">The file or resource path affected by the action (optional).</param>
    /// <param name="description">A human-readable description of the action.</param>
    /// <param name="additionalData">Additional key-value data to include in the log (optional).</param>
    void LogAudit(string actionType, string? targetPath = null, string? description = null, Dictionary<string, string>? additionalData = null);

    /// <summary>
    /// Asynchronously reads audit log entries from the log file.
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries to return (default: 1000).</param>
    /// <returns>A collection of audit log entries.</returns>
    Task<IEnumerable<Models.LogEntry>> ReadAuditLogsAsync(int maxEntries = 1000);
}
