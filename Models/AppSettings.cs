using System;
using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Represents the application configuration settings.
/// Settings are persisted to local storage and loaded on application startup.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the default root directory for file browsing operations.
    /// </summary>
    public string DefaultRootDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the path where archived files are stored.
    /// </summary>
    public string ArchivePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the directory path for shipped items.
    /// </summary>
    public string ShippedDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the directory where file scan results are stored.
    /// </summary>
    public string FileScansDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the directory where inventory archives are stored.
    /// </summary>
    public string InventoryArchiveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current application theme preference.
    /// Valid values: "FollowSystem", "Light", "Dark", "HighContrast".
    /// Defaults to "FollowSystem".
    /// </summary>
    public string Theme { get; set; } = "FollowSystem";
}