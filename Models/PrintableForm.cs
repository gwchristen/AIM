using System;
using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Represents a complete printable form document.
/// Contains multiple pages, each sized for standard paper.
/// </summary>
public class PrintableForm
{
    /// <summary>
    /// Main header (usually the root directory name like "Ohio" or "I&M")
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Sub-header text (e.g., "Inventory Summary")
    /// </summary>
    public string SubHeader { get; set; } = string.Empty;

    /// <summary>
    /// Date the form was generated
    /// </summary>
    public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

    /// <summary>
    /// Username of the person who generated the form
    /// </summary>
    public string GeneratedBy { get; set; } = Environment.UserName;

    /// <summary>
    /// List of pages that make up this form.
    /// Each page is pre-laid out and ready to print.
    /// </summary>
    public List<PrintablePage> Pages { get; set; } = new();

    /// <summary>
    /// Legacy property for backward compatibility with old code.
    /// Maps to all items across all pages.
    /// </summary>
    public List<PrintableFormItem> Items
    {
        get
        {
            var allItems = new List<PrintableFormItem>();
            foreach (var page in Pages)
            {
                allItems.AddRange(page.Rows);
            }
            return allItems;
        }
        set
        {
            // When Items is set, put all items in the first page
            if (Pages.Count == 0)
            {
                Pages.Add(new PrintablePage());
            }
            Pages[0].Rows = value ?? new List<PrintableFormItem>();
        }
    }
}