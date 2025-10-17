using System;

namespace AIM.Models;

public class ContentItem
{
    public string Name { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public string FullPath { get; set; } = string.Empty;
}