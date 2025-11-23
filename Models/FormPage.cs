using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Represents a single page in a printable form document.
/// Contains a header and a collection of rows to be displayed on the page.
/// </summary>
public class FormPage
{
    /// <summary>
    /// Gets or sets the header text displayed at the top of the page.
    /// </summary>
    public string PageHeader { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the collection of rows to be displayed on this page.
    /// </summary>
    public List<FormRow> Rows { get; set; } = new();
}