using System.Collections.Generic;

namespace AIM.Models;

public class Level2Section
{
    public string Header { get; set; } = string.Empty;
    public List<Level3SubSection> SubSections { get; set; } = new();
}