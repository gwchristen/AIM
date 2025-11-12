using Microsoft.UI.Xaml.Controls; // Required for the 'Symbol' enum

namespace AIM.Models;

public class ContentItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsFolder { get; set; }

    // This new property replaces the converter.
    // It returns the 'Folder' symbol if IsFolder is true, otherwise it returns the 'Document' symbol.
    public Symbol SymbolIcon => IsFolder ? Symbol.Folder : Symbol.Document;
}