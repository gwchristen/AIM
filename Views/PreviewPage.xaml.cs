using AIM.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AIM.Views;

public partial class PreviewPage : UserControl
{
    public PreviewViewModel ViewModel { get; }

    public PreviewPage()
    {
        InitializeComponent();
        ViewModel = new PreviewViewModel();
        DataContext = ViewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
