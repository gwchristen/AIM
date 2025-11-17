using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Represents a Level 3 subsection in a hierarchical form structure.
/// Contains a header and a collection of form rows with content.
/// </summary>
public class Level3SubSection
{
    /// <summary>
    /// Gets or sets the header text for this subsection.
    /// </summary>
    public string Header { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the collection of form rows in this subsection.
    /// </summary>
    public List<FormRow> Rows { get; set; } = new();
}