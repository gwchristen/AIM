using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System.Threading.Tasks; // Ensure this using statement is present

namespace AIM.ViewModels;

public partial class PrintableFormViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IPrintService _printService;

    [ObservableProperty]
    private PrintableForm? _formData;

    public PrintableFormViewModel(INavigationService navigationService, IPrintService printService)
    {
        _navigationService = navigationService;
        _printService = printService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is PrintableForm form)
        {
            FormData = form;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
    }

    [RelayCommand]
    private async Task Print(UIElement elementToPrint) // Changed to async Task
    {
        // THE FIX: Check for both the element and the form data before printing.
        if (elementToPrint != null && FormData != null)
        {
            // Use the form's header as the print job title, with a fallback.
            string jobTitle = !string.IsNullOrEmpty(FormData.Header) ? FormData.Header : "AIM Printable Form";

            // Pass the required jobTitle parameter.
            await _printService.PrintAsync(elementToPrint, jobTitle);
        }
    }
}