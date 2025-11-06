using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AIM.Views;

public partial class InvArchivesPage : UserControl
{
    public InvArchivesPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
