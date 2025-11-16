using System;
using System.Collections.Generic;

namespace AIM.Models;

public class AppSettings
{
    // Directory Settings
    public string DefaultRootDirectory { get; set; } = string.Empty;
    public string ArchivePath { get; set; } = string.Empty;
    public string ShippedDirectory { get; set; } = string.Empty;
    public string FileScansDirectory { get; set; } = string.Empty;
    public string InventoryArchiveDirectory { get; set; } = string.Empty;

    // Security Settings Storage Path
    public string SecurityConfigPath { get; set; } = string.Empty;

    // Theme Settings
    public string Theme { get; set; } = "FollowSystem";  // Add this

    // Other settings
    public string Password { get; set; } = string.Empty;
    public List<string> AuthorizedUsers { get; set; } = new();
}