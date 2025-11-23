namespace AIM.Models;

/// <summary>
/// Defines the type of row for display in a printable form.
/// </summary>
public enum RowType
{
    /// <summary>
    /// Level 2 section header.
    /// </summary>
    Level2Header,
    
    /// <summary>
    /// Level 3 subsection header with green styling (Type A).
    /// </summary>
    Level3Header_A,
    
    /// <summary>
    /// Level 3 subsection header with blue styling (Type B).
    /// </summary>
    Level3Header_B,
    
    /// <summary>
    /// Level 3 subsection header with red styling (Type C).
    /// </summary>
    Level3Header_C,
    
    /// <summary>
    /// Regular file entry row.
    /// </summary>
    File,
    
    /// <summary>
    /// Blank separator row.
    /// </summary>
    Blank
}