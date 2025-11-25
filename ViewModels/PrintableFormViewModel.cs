using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using WinRT;

namespace AIM.ViewModels;

public partial class PrintableFormViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IPrintService _printService;
    private PrintableFormPage? _printPage; // Store reference to the page

    [ObservableProperty]
    private PrintableForm? _formData;

    [ObservableProperty]
    private PrintablePage? _currentPage;

    [ObservableProperty]
    private ObservableCollection<PrintablePage>? _pages;

    [ObservableProperty]
    private int _currentPageIndex = 0;

    [ObservableProperty]
    private bool _canGoToPreviousPage = false;

    [ObservableProperty]
    private bool _canGoToNextPage = false;

    public PrintableFormViewModel(INavigationService navigationService, IPrintService printService)
    {
        _navigationService = navigationService;
        _printService = printService;
    }

    /// <summary>
    /// Called by the page to set itself as the reference for printing.
    /// </summary>
    public void SetPrintPage(PrintableFormPage page)
    {
        _printPage = page;
        System.Diagnostics.Debug.WriteLine("PrintableFormPage reference set in ViewModel");
    }

    public void LoadFormData(PrintableForm form)
    {
        FormData = form;
        Pages = new ObservableCollection<PrintablePage>(form.Pages);

        if (Pages.Count > 0)
        {
            CurrentPageIndex = 0;
            CurrentPage = Pages[0];
            UpdatePageNavigation();
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
    private void PreviousPage()
    {
        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            CurrentPage = Pages![CurrentPageIndex];
            UpdatePageNavigation();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPageIndex < Pages!.Count - 1)
        {
            CurrentPageIndex++;
            CurrentPage = Pages[CurrentPageIndex];
            UpdatePageNavigation();
        }
    }

    /// <summary>
    /// Initiates printing - calls the Windows print dialog.
    /// </summary>
    [RelayCommand]
    private void PrintCurrentPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("PrintCurrentPageCommand executed");

            if (CurrentPage == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: CurrentPage is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Attempting to print page {CurrentPage.PageNumber}");

            // Use the stored reference instead of searching the visual tree
            if (_printPage != null)
            {
                System.Diagnostics.Debug.WriteLine("Found PrintableFormPage reference, calling ShowPrintDialog");
                _printPage.ShowPrintDialog();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ERROR: _printPage reference is null");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in PrintCurrentPageCommand: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void UpdatePageNavigation()
    {
        CanGoToPreviousPage = CurrentPageIndex > 0;
        CanGoToNextPage = Pages != null && CurrentPageIndex < Pages.Count - 1;
    }
}