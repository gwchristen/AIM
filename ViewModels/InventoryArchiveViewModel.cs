using AIM.Models;
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
    // THE FIX: Add a field for the settings service.
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFolderSelected))]
    private string? _selectedFolderToArchive;

    public bool IsFolderSelected => !string.IsNullOrEmpty(SelectedFolderToArchive);

    [ObservableProperty]
    private ObservableCollection<string> _archivedDirectories;

    // THE FIX: The hardcoded _archiveBasePath field is removed.

    // THE FIX: The constructor now accepts ISettingsService. Your DI container will provide it automatically.
    public InventoryArchiveViewModel(IDialogService dialogService, INavigationService navigationService, ISettingsService settingsService)
    {
        _dialogService = dialogService;
        _navigationService = navigationService;
        _settingsService = settingsService; // Store the injected service.
        _archivedDirectories = new ObservableCollection<string>();

        // THE FIX: The hardcoded path initialization is removed from the constructor.
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
        // THE FIX: Load settings to get the archive path instead of using the hardcoded field.
        AppSettings settings = _settingsService.LoadSettings();
        string archiveBasePath = settings.InventoryArchiveDirectory;

        if (string.IsNullOrEmpty(archiveBasePath))
        {
            // If the path isn't set, we do nothing. This is expected if the user hasn't configured it.
            return;
        }

        try
        {
            Directory.CreateDirectory(archiveBasePath); // Ensures the directory exists.
            var dirs = Directory.GetDirectories(archiveBasePath).Select(Path.GetFileName);
            foreach (var dir in dirs)
            {
                if (dir != null) ArchivedDirectories.Add(dir);
            }
        }
        catch (Exception ex)
        {
            // Keep the method synchronous by not awaiting the dialog task.
            _ = _dialogService.ShowErrorDialogAsync("Error Loading Archives", $"Could not read the archive directory at '{archiveBasePath}'.\nError: {ex.Message}");
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

        // THE FIX: Load settings to get the configured archive path.
        AppSettings settings = _settingsService.LoadSettings();
        string archiveBasePath = settings.InventoryArchiveDirectory;

        if (string.IsNullOrEmpty(archiveBasePath))
        {
            await _dialogService.ShowInfoDialog("Archive Directory Not Set", "The Inventory Archive Directory has not been configured in the settings. Cannot archive the folder.");
            return;
        }

        var (result, newName) = await _dialogService.ShowTextInputDialog("Name Your Archive", "Enter a name for this archive:", Path.GetFileName(SelectedFolderToArchive));

        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(newName))
        {
            string destinationPath = Path.Combine(archiveBasePath, newName);

            try
            {
                if (Directory.Exists(destinationPath))
                {
                    await _dialogService.ShowErrorDialogAsync("Archive Exists", $"An archive with the name '{newName}' already exists. Please choose a different name.");
                    return;
                }

                Directory.Move(SelectedFolderToArchive, destinationPath);
                await _dialogService.ShowSuccessDialog("Archive Complete", $"The folder has been successfully archived as '{newName}'.");

                SelectedFolderToArchive = null;
                // Reload the directory list. This method is synchronous.
                LoadArchivedDirectories();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync("Archiving Failed", $"An error occurred while moving the folder.\nError: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void ViewArchivedFolder(string? folderName)
    {
        if (string.IsNullOrEmpty(folderName)) return;

        // THE FIX: Load settings to ensure the correct path is used for navigation.
        AppSettings settings = _settingsService.LoadSettings();
        string archiveBasePath = settings.InventoryArchiveDirectory;

        if (string.IsNullOrEmpty(archiveBasePath))
        {
            _ = _dialogService.ShowErrorDialogAsync("Archive Path Missing", "The Inventory Archive Directory is not set in settings. Cannot open the folder.");
            return;
        }

        string fullPath = Path.Combine(archiveBasePath, folderName);
        _navigationService.NavigateTo(typeof(Views.InventoryViewerPage), fullPath);
    }
}