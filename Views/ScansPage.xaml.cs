using AIM.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AIM.Views;

public partial class ScansPage : UserControl
{
    public ScansViewModel ViewModel { get; }

    public ScansPage()
    {
        InitializeComponent();
        ViewModel = new ScansViewModel();
        DataContext = ViewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
