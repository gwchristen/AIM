namespace AIM.Models;

/// <summary>
/// Represents a file that has been identified as anomalous or misplaced in the inventory.
/// </summary>
public class FileAnomalyItem
{
    /// <summary>
    /// Gets or sets the name of the anomalous file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the full path to the anomalous file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of anomaly detected (e.g., "Misplaced OH", "Misplaced IM", "Unidentified").
    /// </summary>
    public string AnomalyType { get; set; } = string.Empty;
}