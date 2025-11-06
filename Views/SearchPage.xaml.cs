using AIM.Models;
using AIM.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AIM.Views;

public partial class SearchPage : UserControl
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = new SearchViewModel();
        DataContext = ViewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
