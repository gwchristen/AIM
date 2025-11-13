namespace AIM.Models;

public enum RowType
{
    Level2Header,
    Level3Header,
    File,
    Blank
}

public class FormRow
{
    public string Content { get; set; } = string.Empty;
    public RowType Type { get; set; }
}