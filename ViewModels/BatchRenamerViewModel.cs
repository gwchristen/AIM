using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class BatchRenamerViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameFilesCommand))]
    private string? _renameDirectory;

    public BatchRenamerViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
    }

    private bool CanRenameFiles() => !string.IsNullOrEmpty(RenameDirectory);

    [RelayCommand]
    private async Task SelectRenameDirectoryAsync() => RenameDirectory = await PickFolderAsync();

    [RelayCommand(CanExecute = nameof(CanRenameFiles))]
    private async Task RenameFilesAsync()
    {
        bool confirmed = await _dialogService.ShowConfirmationDialogAsync("Confirm Mass Rename", "This will rename all files within the selected directory's OpCo subfolders.\nThis action is permanent and cannot be undone easily.\n\nAre you sure you want to continue?");
        if (!confirmed) return;

        try
        {
            var summary = await _directoryOperationService.RenameFilesSequentiallyAsync(RenameDirectory!);
            var summaryText = new StringBuilder("Renaming complete.\n\nSummary:\n");
            if (summary.Any())
            {
                foreach (var entry in summary) summaryText.AppendLine($"- {entry.Key}: {entry.Value} files renamed.");
            }
            else
            {
                summaryText.AppendLine("No files were found to rename.");
            }
            await _dialogService.ShowSuccessDialog("Success", summaryText.ToString());
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Renaming Failed", $"An error occurred during the renaming process.\nError: {ex.Message}");
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