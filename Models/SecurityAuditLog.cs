using System;

namespace AIM.Models;

/// <summary>
/// Represents a security audit log entry in the AIM security database.
/// </summary>
public class SecurityAuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit log entry.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// Gets or sets the action type (e.g., 'ADD_USER', 'REMOVE_USER', 'MODIFY_USER', 'PASSWORD_CHANGE').
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target user affected by this action.
    /// </summary>
    public string? TargetUser { get; set; }

    /// <summary>
    /// Gets or sets the username of who performed this action.
    /// </summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details about the action.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets when this action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
