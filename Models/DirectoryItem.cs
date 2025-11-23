using System.Collections.ObjectModel;

namespace AIM.Models;

/// <summary>
/// Represents a directory node in the file tree structure.
/// Contains the directory name, full path, and its subdirectories.
/// </summary>
public class DirectoryItem
{
    /// <summary>
    /// Gets or sets the directory name.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the full path to the directory.
    /// </summary>
    public string FullPath { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of subdirectories.
    /// </summary>
    public ObservableCollection<DirectoryItem> SubDirectories { get; set; } = new();
}