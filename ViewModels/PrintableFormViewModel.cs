using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;

namespace AIM.ViewModels;

public partial class PrintableFormViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private PrintableForm? _formData;

    public PrintableFormViewModel()
    {
        // This ensures the ViewModel gets the *single, correct* instance of the NavigationService.
        _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
    }
}