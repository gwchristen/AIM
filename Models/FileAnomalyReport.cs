using System.Collections.Generic;

namespace AIM.Models;

public class FileAnomalyReport
{
    public List<string> MisplacedOhFiles { get; set; } = new();
    public List<string> MisplacedImFiles { get; set; } = new();
    public List<string> UnidentifiedFiles { get; set; } = new();
}