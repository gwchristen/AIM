using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace AIM.Models;

/// <summary>
/// Represents an item in the scans tree, which can be a folder or a file.
/// Folders can contain other ScanTreeItems in a hierarchical structure.
/// This class is observable to support UI binding and property change notifications.
/// </summary>
public partial class ScanTreeItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the name of the file or folder.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the full path to the file or folder.
    /// </summary>
    public string FullPath { get; set; }
    
    /// <summary>
    /// Gets or sets whether this item is a folder (true) or a file (false).
    /// </summary>
    public bool IsFolder { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the file in bytes. Zero for folders.
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// Gets or sets the last modified date of the item.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Gets or sets whether this tree node is expanded in the UI.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Gets or sets the collection of child items for folder nodes.
    /// </summary>
    public ObservableCollection<ScanTreeItem> Children { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this node has children that haven't been loaded yet.
    /// Used by the TreeView to show/hide the expander icon.
    /// </summary>
    [ObservableProperty]
    private bool _hasUnrealizedChildren = true;

    /// <summary>
    /// Gets or sets whether this tree node is currently selected in the UI.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets whether this item is in rename mode.
    /// </summary>
    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>
    /// Gets the formatted size string for display in the UI.
    /// Returns empty string for folders.
    /// </summary>
    public string SizeString => IsFolder ? "" : $"{Size / 1024:N0} KB";
    
    /// <summary>
    /// Gets the formatted date string for display in the UI.
    /// </summary>
    public string ModifiedDateString => ModifiedDate.ToString("g");
}