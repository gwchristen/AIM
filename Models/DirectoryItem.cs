using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AIM.Models;

public partial class DirectoryItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsExpanded { get; set; } = false;
    public ObservableCollection<DirectoryItem> SubDirectories { get; } = new();
}