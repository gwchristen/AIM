using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class DirClonerViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IDirectoryOperationService _directoryOperationService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateStructureCommand))]
    private string? _sourceDirectory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateStructureCommand))]
    private string? _destinationDirectory;

    public DirClonerViewModel(IDialogService dialogService, IDirectoryOperationService directoryOperationService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
    }

    private bool CanCreateStructure() => !string.IsNullOrEmpty(SourceDirectory) && !string.IsNullOrEmpty(DestinationDirectory);

    [RelayCommand]
    private async Task SelectSourceAsync() => SourceDirectory = await PickFolderAsync();

    [RelayCommand]
    private async Task SelectDestinationAsync() => DestinationDirectory = await PickFolderAsync();

    [RelayCommand(CanExecute = nameof(CanCreateStructure))]
    private async Task CreateStructureAsync()
    {
        var (result, newName) = await _dialogService.ShowTextInputDialog("Enter New Directory Name", "Please provide a name for the new directory structure.");
        if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary || string.IsNullOrWhiteSpace(newName)) return;

        try
        {
            await _directoryOperationService.CopyDirectoryStructureAsync(SourceDirectory!, DestinationDirectory!, newName);
            await _dialogService.ShowSuccessDialog("Success", $"The directory structure was successfully created at '{System.IO.Path.Combine(DestinationDirectory!, newName)}'.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Operation Failed", $"Could not create the directory structure.\nError: {ex.Message}");
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