using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Represents a Level 2 section in a hierarchical form structure.
/// Contains a header and a collection of Level 3 subsections.
/// </summary>
public class Level2Section
{
    /// <summary>
    /// Gets or sets the header text for this section.
    /// </summary>
    public string Header { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the collection of Level 3 subsections within this section.
    /// </summary>
    public List<Level3SubSection> SubSections { get; set; } = new();
}