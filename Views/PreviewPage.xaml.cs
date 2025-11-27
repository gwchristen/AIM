using AIM.Services;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class PreviewPage : Page
{
    public PreviewViewModel ViewModel { get; }
    private readonly INavigationService _navigationService;

    public PreviewPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<PreviewViewModel>();
        _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedTo(e.Parameter);
    }

    private void GoBackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
    }
}