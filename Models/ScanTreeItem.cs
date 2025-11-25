using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace AIM.Models;

/// <summary>
/// Represents an item in the scans tree, which can be a folder or a file.
/// Folders can contain other ScanTreeItems.
/// </summary>
public partial class ScanTreeItem : ObservableObject
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsFolder { get; set; }
    public long Size { get; set; }
    public DateTime ModifiedDate { get; set; }

    [ObservableProperty]
    private bool _isExpanded;

    // This property holds the children for a folder node.
    public ObservableCollection<ScanTreeItem> Children { get; set; } = new();

    // This property is used by the TreeView to show/hide the expander icon.
    [ObservableProperty]
    private bool _hasUnrealizedChildren = true;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isRenaming;

    // FIX: Removed the duplicate definitions. These are the ones we will keep.
    public string SizeString => IsFolder ? "" : $"{Size / 1024:N0} KB";
    public string ModifiedDateString => ModifiedDate.ToString("g");
}