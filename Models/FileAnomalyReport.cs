using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Contains the results of a file anomaly analysis scan.
/// Groups anomalies by type for easy reporting and review.
/// </summary>
public class FileAnomalyReport
{
    /// <summary>
    /// Gets or sets the list of Ohio (OH) files found in incorrect locations.
    /// </summary>
    public List<string> MisplacedOhFiles { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of I&M files found in incorrect locations.
    /// </summary>
    public List<string> MisplacedImFiles { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of files that could not be categorized.
    /// </summary>
    public List<string> UnidentifiedFiles { get; set; } = new();
}