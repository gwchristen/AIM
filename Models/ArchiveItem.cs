using System;

namespace AIM.Models;

public class ArchiveItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime DateArchived { get; set; }
    public long Size { get; set; }
    public int FileCount { get; set; }
    public int FolderCount { get; set; }

    public string DateArchivedText => $"Archived {DateArchived:MMM dd, yyyy}";

    public string SizeText
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = Size;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    public string DetailText => $"{FolderCount} folders, {FileCount} files";
}