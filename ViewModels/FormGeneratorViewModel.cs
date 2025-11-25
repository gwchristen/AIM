using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class FormGeneratorViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly FormTemplateFactory _templateFactory;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateFormCommand))]
    private string? _formDirectory;

    [ObservableProperty]
    private string _selectedTemplate = "Ohio";

    [ObservableProperty]
    private List<string> _availableTemplates;

    public FormGeneratorViewModel(IDialogService dialogService, FormTemplateFactory templateFactory, INavigationService navigationService)
    {
        _dialogService = dialogService;
        _templateFactory = templateFactory;
        _navigationService = navigationService;

        // Load available templates
        AvailableTemplates = new List<string>(_templateFactory.GetAvailableTemplates());
    }

    private bool CanGenerateForm() => !string.IsNullOrEmpty(FormDirectory);

    [RelayCommand]
    private async Task SelectFormDirectoryAsync() => FormDirectory = await PickFolderAsync();

    [RelayCommand(CanExecute = nameof(CanGenerateForm))]
    private async Task GenerateFormAsync()
    {
        try
        {
            // Get the selected template
            var template = _templateFactory.GetTemplate(SelectedTemplate);

            // Generate form data using the template
            var formData = await template.GenerateAsync(FormDirectory!);

            // Navigate to the printable page
            _navigationService.NavigateTo(typeof(Views.PrintableFormPage), formData);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Form Generation Failed", $"Could not generate the form data.\nError: {ex.Message}");
        }
    }

    private async Task<string?> PickFolderAsync()
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            FileTypeFilter = { "*" }
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }
}