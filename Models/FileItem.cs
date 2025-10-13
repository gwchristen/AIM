using System;

namespace AIM.Models;

public enum FileType { Text, Csv, Log, Other }

public class FileItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public FileType Type { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string ContentPreview { get; set; } = string.Empty;
}