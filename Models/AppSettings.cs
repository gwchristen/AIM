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
    /// Gets or sets the file path to the encrypted security configuration.
    /// This file contains the master password and authorized users list.
    /// </summary>
    public string SecurityConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current application theme preference.
    /// Valid values: "FollowSystem", "Light", "Dark", "HighContrast".
    /// Defaults to "FollowSystem".
    /// </summary>
    public string Theme { get; set; } = "FollowSystem";

    /// <summary>
    /// Gets or sets the application password.
    /// This property is deprecated; use SecurityConfigPath for encrypted password storage instead.
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of authorized user IDs.
    /// This property is deprecated; use SecurityConfigPath for encrypted user list storage instead.
    /// </summary>
    public List<string> AuthorizedUsers { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the initial master password has been set.
    /// When false, the application requires the user to set a master password on first launch.
    /// This ensures no default or hardcoded passwords are used in production.
    /// </summary>
    public bool IsInitialPasswordSet { get; set; } = false;

    /// <summary>
    /// Gets or sets the path to the shared security configuration.
    /// This path is used to locate the centrally managed security configuration
    /// when UseSharedConfig is enabled. Can be overridden by security-config.ini.
    /// </summary>
    public string SharedSecurityConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use shared network configuration.
    /// When true, the application will attempt to load security configuration
    /// from the shared network path specified in SharedSecurityConfigPath or
    /// from the security-config.ini file.
    /// </summary>
    public bool UseSharedConfig { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the centralized SQLite security database.
    /// This database stores authorized users, security settings, and audit logs.
    /// All AIM instances read from this shared database for centralized user management.
    /// </summary>
    public string SecurityDatabasePath { get; set; } = string.Empty;
}