namespace AIM.Models;

/// <summary>
/// Represents an item in the breadcrumb navigation trail.
/// Used to display the current path hierarchy in the UI.
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// Gets or sets the display name of the breadcrumb item.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the full path this breadcrumb item represents.
    /// </summary>
    public string FullPath { get; set; }
    
    /// <summary>
    /// Gets or sets whether this is the last item in the breadcrumb trail.
    /// </summary>
    public bool IsLast { get; set; }
}