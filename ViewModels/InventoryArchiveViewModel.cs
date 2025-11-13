using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class InventoryArchiveViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFolderSelected))]
    private string? _selectedFolderToArchive;

    public bool IsFolderSelected => !string.IsNullOrEmpty(SelectedFolderToArchive);

    [ObservableProperty]
    private ObservableCollection<string> _archivedDirectories;

    private readonly string _archiveBasePath;

    public InventoryArchiveViewModel(IDialogService dialogService, INavigationService navigationService)
    {
        _dialogService = dialogService;
        _navigationService = navigationService;
        _archivedDirectories = new ObservableCollection<string>();

        _archiveBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AIM_Archives");
    }

    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            FileTypeFilter = { "*" }
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            SelectedFolderToArchive = folder.Path;
        }
    }

    [RelayCommand]
    private void LoadArchivedDirectories()
    {
        ArchivedDirectories.Clear();
        try
        {
            Directory.CreateDirectory(_archiveBasePath);
            var dirs = Directory.GetDirectories(_archiveBasePath).Select(Path.GetFileName);
            foreach (var dir in dirs)
            {
                if (dir != null) ArchivedDirectories.Add(dir);
            }
        }
        catch (Exception ex)
        {
            // THE FIX: Calling your existing error dialog method
            _dialogService.ShowErrorDialogAsync("Error Loading Archives", $"Could not read the archive directory at '{_archiveBasePath}'.\nError: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ArchiveFolderAsync()
    {
        if (string.IsNullOrEmpty(SelectedFolderToArchive))
        {
            await _dialogService.ShowInfoDialog("No Folder Selected", "Please select a folder to archive first.");
            return;
        }

        var (result, newName) = await _dialogService.ShowTextInputDialog("Name Your Archive", "Enter a name for this archive:", Path.GetFileName(SelectedFolderToArchive));

        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(newName))
        {
            string destinationPath = Path.Combine(_archiveBasePath, newName);

            try
            {
                if (Directory.Exists(destinationPath))
                {
                    // THE FIX: Calling your existing error dialog method
                    await _dialogService.ShowErrorDialogAsync("Archive Exists", $"An archive with the name '{newName}' already exists. Please choose a different name.");
                    return;
                }

                Directory.Move(SelectedFolderToArchive, destinationPath);
                await _dialogService.ShowSuccessDialog("Archive Complete", $"The folder has been successfully archived as '{newName}'.");

                SelectedFolderToArchive = null;
                LoadArchivedDirectories();
            }
            catch (Exception ex)
            {
                // THE FIX: Calling your existing error dialog method
                await _dialogService.ShowErrorDialogAsync("Archiving Failed", $"An error occurred while moving the folder.\nError: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void ViewArchivedFolder(string? folderName)
    {
        if (string.IsNullOrEmpty(folderName)) return;

        string fullPath = Path.Combine(_archiveBasePath, folderName);
        _navigationService.NavigateTo(typeof(Views.InventoryViewerPage), fullPath);
    }
}