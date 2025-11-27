using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Xml.Linq;
using Windows.UI;

namespace AIM.Models;

public partial class ScanTreeItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private long _size;

    [ObservableProperty]
    private DateTime _modifiedDate;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isRenaming;

    [ObservableProperty]
    private bool _isPersistentlySelected;

    public string ModifiedDateString => ModifiedDate.ToString("g");

    public string SizeString => IsFolder ? "" : FormatFileSize(Size);

    public string Extension => IsFolder ? "" : Path.GetExtension(FullPath).ToUpperInvariant();

    public string FileIcon => IsFolder ? "\uE8B7" : Extension.ToLower() switch
    {
        ".csv" => "\uE9D9",
        ". txt" => "\uE8A5",
        _ => "\uE8A5"
    };

    public SolidColorBrush IconColor => IsFolder
        ? new SolidColorBrush(Color.FromArgb(255, 255, 183, 77))  // Folder yellow
        : Extension.ToLower() switch
        {
            ".csv" => new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),  // Green for CSV
            ".txt" => new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)), // Blue for TXT
            _ => new SolidColorBrush(Color.FromArgb(255, 158, 158, 158))      // Gray for others
        };

    public string PreviewTooltip
    {
        get
        {
            if (IsFolder)
            {
                return $"Folder: {Name}\nPath: {FullPath}\nModified: {ModifiedDateString}";
            }

            try
            {
                var content = File.ReadAllText(FullPath);
                var preview = content.Length > 500 ? content.Substring(0, 500) + "..." : content;
                return $"File: {Name}\nSize: {SizeString}\nModified: {ModifiedDateString}\n\n--- Preview ---\n{preview}";
            }
            catch
            {
                return $"File: {Name}\nSize: {SizeString}\nModified: {ModifiedDateString}\n\n(Preview unavailable)";
            }
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}