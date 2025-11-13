using System.Collections.Generic;

namespace AIM.Models;

public class PrintableForm
{
    public string OpCoHeader { get; set; } = string.Empty;
    public List<FormPage> Pages { get; set; } = new();
}