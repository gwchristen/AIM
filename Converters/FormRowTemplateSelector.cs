using AIM.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Converters;

public class FormRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Level2HeaderTemplate { get; set; }
    public DataTemplate? Level3HeaderTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }
    public DataTemplate? BlankTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is FormRow row)
        {
            return row.Type switch
            {
                RowType.Level2Header => Level2HeaderTemplate,
                RowType.Level3Header => Level3HeaderTemplate,
                RowType.File => FileTemplate,
                RowType.Blank => BlankTemplate,
                _ => base.SelectTemplateCore(item, container)
            };
        }
        return base.SelectTemplateCore(item, container);
    }
}