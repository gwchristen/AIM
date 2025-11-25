using AIM.Models;
using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Represents a single page in a printable form.
/// Each page is designed to fit on standard 8.5x11" paper.
/// </summary>
public class PrintablePage
{
    /// <summary>
    /// Header text that appears at the top of every page (e.g., "Ohio", "I&M")
    /// </summary>
    public string PageHeader { get; set; } = string.Empty;

    /// <summary>
    /// The Level 2 header for this page (e.g., "Section A", "Region 1")
    /// Repeats on continuation pages if needed.
    /// </summary>
    public string Level2Header { get; set; } = string.Empty;

    /// <summary>
    /// All rows to display on this page, including headers, files, and blanks.
    /// </summary>
    public List<PrintableFormItem> Rows { get; set; } = new();

    /// <summary>
    /// Current page number in the sequence.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Total number of pages in the document.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether this is a continuation page (Level2Header repeats if true).
    /// </summary>
    public bool IsContinuationPage { get; set; }
}