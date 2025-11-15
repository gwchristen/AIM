using AIM.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Converters; // Correct namespace based on your file path

public class FormRowTemplateSelector : DataTemplateSelector
{
    // Properties to hold the templates defined in XAML
    public DataTemplate? Level2HeaderTemplate { get; set; }
    public DataTemplate? Level3Header_A_Template { get; set; }
    public DataTemplate? Level3Header_B_Template { get; set; }
    public DataTemplate? Level3Header_C_Template { get; set; }
    public DataTemplate? FileTemplate { get; set; }
    public DataTemplate? BlankTemplate { get; set; }

    // This is the core logic that runs for each item in the ListView
    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is PrintableFormItem formItem)
        {
            // THE FIX: This switch statement now uses the new, specific RowType enums.
            // This resolves the error because 'Level3Header' is no longer referenced.
            return formItem.Type switch
            {
                RowType.Level2Header => Level2HeaderTemplate,
                RowType.Level3Header_A => Level3Header_A_Template,
                RowType.Level3Header_B => Level3Header_B_Template,
                RowType.Level3Header_C => Level3Header_C_Template,
                RowType.File => FileTemplate,
                _ => BlankTemplate, // Default to a blank row
            };
        }

        return base.SelectTemplateCore(item);
    }
}