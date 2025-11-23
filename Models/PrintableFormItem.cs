namespace AIM.Models;

/// <summary>
/// Represents a single displayable line in the final printable form.
/// </summary>
public class PrintableFormItem
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