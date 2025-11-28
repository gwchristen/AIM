using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using Windows.UI;

namespace AIM.Models;

public class ContentItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsFolder { get; set; }

    public long Size { get; set; }
    public DateTime ModifiedDate { get; set; }

    // Legacy property for backwards compatibility
    public Symbol SymbolIcon => IsFolder ? Symbol.Folder : Symbol.Document;

    // New icon properties matching ScanTreeItem style
    public string FileIcon
    {
        get
        {
            if (IsFolder) return "\uE8B7"; // Folder icon

            var ext = Path.GetExtension(Name)?.ToLowerInvariant();
            return ext switch
            {
                ". txt" => "\uE8A5",  // Document icon
                ".csv" => "\uE9D9",  // Table/spreadsheet icon
                _ => "\uE8A5"        // Default document icon
            };
        }
    }

    public SolidColorBrush IconColor
    {
        get
        {
            if (IsFolder) return new SolidColorBrush(Color.FromArgb(255, 255, 183, 77)); // Orange/gold for folders

            var ext = Path.GetExtension(Name)?.ToLowerInvariant();
            return ext switch
            {
                ".txt" => new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)),   // Blue for text files
                ".csv" => new SolidColorBrush(Color.FromArgb(255, 16, 124, 16)),   // Green for CSV files
                _ => new SolidColorBrush(Color.FromArgb(255, 128, 128, 128))       // Gray for other files
            };
        }
    }

    public string SizeString => IsFolder ? "" : $"{Size / 1024.0:F2} KB";
    public string ModifiedDateString => ModifiedDate == default ? "" : ModifiedDate.ToString("d");
}