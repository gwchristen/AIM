using System.Collections.ObjectModel;

namespace AIM.Models;

public class DirectoryItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public ObservableCollection<DirectoryItem> SubDirectories { get; set; } = new();
    public ObservableCollection<FileItem> Files { get; set; } = new();
}