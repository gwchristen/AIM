namespace AIM.Models;

/// <summary>
/// Defines the type of row for display in a printable form.
/// </summary>
public enum RowType
{
    Level2Header,
    // THE FIX: More specific Level 3 headers for different colors
    Level3Header_A, // For green
    Level3Header_B, // For blue
    Level3Header_C, // For red
    File,
    Blank
}