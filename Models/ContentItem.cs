using Microsoft.UI.Xaml.Controls; // Required for the 'Symbol' enum
using System;                     // Required for DateTime

namespace AIM.Models;

public class ContentItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsFolder { get; set; }

    // THE FIX: Add properties for sorting and display
    public long Size { get; set; }
    public DateTime ModifiedDate { get; set; }

    public Symbol SymbolIcon => IsFolder ? Symbol.Folder : Symbol.Document;

    // THE FIX: Add string properties for easy binding in the DataGrid
    public string SizeString => IsFolder ? "" : $"{Size / 1024.0:F2} KB";
    public string ModifiedDateString => ModifiedDate == default ? "" : ModifiedDate.ToString("d");
}