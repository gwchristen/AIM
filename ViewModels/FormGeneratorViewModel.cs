using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class FormGeneratorViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateFormCommand))]
    private string? _formDirectory;

    public FormGeneratorViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService, INavigationService navigationService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
        _navigationService = navigationService;
    }

    private bool CanGenerateForm() => !string.IsNullOrEmpty(FormDirectory);

    [RelayCommand]
    private async Task SelectFormDirectoryAsync() => FormDirectory = await PickFolderAsync();

    [RelayCommand(CanExecute = nameof(CanGenerateForm))]
    private async Task GenerateFormAsync()
    {
        try
        {
            var formData = await _directoryOperationService.GenerateFormDataAsync(FormDirectory!);
            // Navigate to the printable page and pass the generated data
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