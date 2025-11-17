namespace AIM.Models;

/// <summary>
/// Represents a file that has been identified as problematic during inventory analysis.
/// Problematic files may have naming issues, be in incorrect locations, or have other anomalies.
/// </summary>
public class ProblematicFile
{
    /// <summary>
    /// Gets or sets the full path to the problematic file.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}