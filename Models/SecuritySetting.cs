using System;

namespace AIM.Models;

/// <summary>
/// Represents a security setting in the AIM security database.
/// </summary>
public class SecuritySetting
{
    /// <summary>
    /// Gets or sets the setting key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username of who last modified this setting.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets when this setting was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
