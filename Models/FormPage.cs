using System.Collections.Generic;

namespace AIM.Models;

public class FormPage
{
    public string PageHeader { get; set; } = string.Empty;
    public List<FormRow> Rows { get; set; } = new();
}