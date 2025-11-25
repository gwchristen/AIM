using AIM.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Converters;

public class FormRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Level2HeaderTemplate { get; set; }
    public DataTemplate? Level3Header_A_Template { get; set; }
    public DataTemplate? Level3Header_B_Template { get; set; }
    public DataTemplate? Level3Header_C_Template { get; set; }
    public DataTemplate? FileTemplate { get; set; }
    public DataTemplate? BlankTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is PrintableFormItem formItem)
        {
            return formItem.Type switch
            {
                RowType.Level2Header => Level2HeaderTemplate,
                RowType.Level3Header_A => Level3Header_A_Template,
                RowType.Level3Header_B => Level3Header_B_Template,
                RowType.Level3Header_C => Level3Header_C_Template,
                RowType.File => FileTemplate,
                RowType.Blank => BlankTemplate,
                _ => BlankTemplate,
            };
        }
        return base.SelectTemplateCore(item);
    }
}