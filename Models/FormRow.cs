namespace AIM.Models;

/// <summary>
/// Represents a single row in a printable form.
/// Contains the text content and the row type for formatting.
/// </summary>
public class FormRow
{
    /// <summary>
    /// Gets or sets the text content to be displayed in this row.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of row, which determines its formatting and styling.
    /// </summary>
    public RowType Type { get; set; }
}