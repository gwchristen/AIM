using System;

namespace AIM.Models;

/// <summary>
/// Represents a single entry in the audit log.
/// Captures user actions with timestamp and details for compliance and tracking purposes.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the Windows username of the user who performed the action.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of action performed (e.g., "FILE_MOVE", "AUTH_LOGIN_SUCCESS").
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets additional details about the action in JSON format.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}