using AIM.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Services;

/// <summary>
/// Selects the appropriate DataTemplate for a PrintableFormItem based on its row type.
/// Provides distinct templates for different header levels and content types.
/// </summary>
public class RowTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template for Level 2 section headers.
    /// </summary>
    public DataTemplate? Level2HeaderTemplate { get; set; }
    
    /// <summary>
    /// Gets or sets the template for Level 3 subsection headers (Type A - green styling).
    /// </summary>
    public DataTemplate? Level3Header_A_Template { get; set; }
    
    /// <summary>
    /// Gets or sets the template for Level 3 subsection headers (Type B - blue styling).
    /// </summary>
    public DataTemplate? Level3Header_B_Template { get; set; }
    
    /// <summary>
    /// Gets or sets the template for Level 3 subsection headers (Type C - red styling).
    /// </summary>
    public DataTemplate? Level3Header_C_Template { get; set; }
    
    /// <summary>
    /// Gets or sets the template for file entry rows.
    /// </summary>
    public DataTemplate? FileTemplate { get; set; }
    
    /// <summary>
    /// Gets or sets the template for blank separator rows.
    /// </summary>
    public DataTemplate? BlankTemplate { get; set; }

    /// <summary>
    /// Selects the appropriate DataTemplate based on the PrintableFormItem's row type.
    /// </summary>
    /// <param name="item">The PrintableFormItem to select a template for.</param>
    /// <returns>The DataTemplate that should be used to display the item.</returns>
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
                _ => BlankTemplate,
            };
        }
        return base.SelectTemplateCore(item);
    }
}