using Microsoft.UI.Xaml.Controls; // Required for the 'Symbol' enum
using System;                     // Required for DateTime

namespace AIM.Models;

public class ContentItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsFolder { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the last modified date of the item.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Gets the symbol icon for this item (Folder or Document).
    /// </summary>
    public Symbol SymbolIcon => IsFolder ? Symbol.Folder : Symbol.Document;

    /// <summary>
    /// Gets the formatted size string for display in the UI.
    /// Returns empty string for folders.
    /// </summary>
    public string SizeString => IsFolder ? "" : $"{Size / 1024.0:F2} KB";

    /// <summary>
    /// Gets the formatted date string for display in the UI.
    /// </summary>
    public string ModifiedDateString => ModifiedDate == default ? "" : ModifiedDate.ToString("d");
}