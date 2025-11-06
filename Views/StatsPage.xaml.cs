using AIM.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AIM.Views;

public partial class StatsPage : UserControl
{
    public StatsViewModel ViewModel { get; }

    public StatsPage()
    {
        InitializeComponent();
        ViewModel = new StatsViewModel(MainWindow.Instance!.ViewModel);
        DataContext = ViewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
