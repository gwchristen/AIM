using System.Collections.ObjectModel;

namespace AIM.Models;

public class DirectoryItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public ObservableCollection<DirectoryItem> SubDirectories { get; set; } = new();
}