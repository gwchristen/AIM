using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<SearchViewModel>();
        // DataContext is set by x:Bind in the XAML, so no need to set it here.
    }
}