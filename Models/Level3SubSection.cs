using System.Collections.Generic;

namespace AIM.Models;

public class Level3SubSection
{
    public string Header { get; set; } = string.Empty;
    public List<FormRow> Rows { get; set; } = new();
}