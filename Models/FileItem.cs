using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AIM.Models;

public partial class FileItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public FileType Type { get; set; }
    public long Size { get; set; }
    public string SizeString { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedDateString { get; set; } = string.Empty;
    public string ModifiedDateString { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = string.Empty;
}

public enum FileType
{
    Text,
    Csv,
    Log,
    Other
}