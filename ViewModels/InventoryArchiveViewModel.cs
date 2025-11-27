using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
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
    private readonly ISettingsService _settingsService;
    private readonly IInfoBarService _infoBarService;

    #region Observable Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFolderSelected))]
    private string _selectedFolderToArchive;

    [ObservableProperty]
    private bool _isArchiving;

    [ObservableProperty]
    private string _archiveProgressText = "Archiving...";

    [ObservableProperty]
    private bool _isLoadingArchives;

    [ObservableProperty]
    private bool _hasArchives;

    [ObservableProperty]
    private bool _showEmptyState;

    [ObservableProperty]
    private int _archiveCount;
    #endregion

    public bool IsFolderSelected => !string.IsNullOrEmpty(SelectedFolderToArchive);

    public ObservableCollection<ArchiveItem> ArchivedDirectories { get; } = new();

    public InventoryArchiveViewModel(
        IDialogService dialogService,
        INavigationService navigationService,
        ISettingsService settingsService,
        IInfoBarService infoBarService)
    {
        _dialogService = dialogService;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _infoBarService = infoBarService;
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
    private async Task LoadArchivedDirectoriesAsync()
    {
        IsLoadingArchives = true;
        ArchivedDirectories.Clear();

        try
        {
            AppSettings settings = _settingsService.LoadSettings();
            string archiveBasePath = settings.InventoryArchiveDirectory;

            if (string.IsNullOrEmpty(archiveBasePath))
            {
                ShowEmptyState = true;
                HasArchives = false;
                return;
            }

            await Task.Run(() =>
            {
                Directory.CreateDirectory(archiveBasePath);
            });

            var directories = await Task.Run(() => Directory.GetDirectories(archiveBasePath));

            foreach (var dirPath in directories)
            {
                var dirInfo = new DirectoryInfo(dirPath);

                // Calculate size and counts
                long size = 0;
                int fileCount = 0;
                int folderCount = 0;

                try
                {
                    await Task.Run(() =>
                    {
                        var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                        size = files.Sum(f => f.Length);
                        fileCount = files.Length;
                        folderCount = dirInfo.GetDirectories("*", SearchOption.AllDirectories).Length;
                    });
                }
                catch { }

                ArchivedDirectories.Add(new ArchiveItem
                {
                    Name = dirInfo.Name,
                    FullPath = dirPath,
                    DateArchived = dirInfo.CreationTime,
                    Size = size,
                    FileCount = fileCount,
                    FolderCount = folderCount
                });
            }

            ArchiveCount = ArchivedDirectories.Count;
            HasArchives = ArchivedDirectories.Count > 0;
            ShowEmptyState = !HasArchives && !IsLoadingArchives;
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not load archives: {ex.Message}", InfoBarSeverity.Error);
            ShowEmptyState = true;
            HasArchives = false;
        }
        finally
        {
            IsLoadingArchives = false;
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

        AppSettings settings = _settingsService.LoadSettings();
        string archiveBasePath = settings.InventoryArchiveDirectory;

        if (string.IsNullOrEmpty(archiveBasePath))
        {
            await _dialogService.ShowInfoDialog("Archive Directory Not Set",
                "The Inventory Archive Directory has not been configured in Settings.");
            return;
        }

        // Get folder info for confirmation
        var folderInfo = new DirectoryInfo(SelectedFolderToArchive);
        var folderName = folderInfo.Name;

        var (result, newName) = await _dialogService.ShowTextInputDialog(
            "Archive Folder",
            $"Enter a name for this archive:\n\nSource: {SelectedFolderToArchive}",
            folderName);

        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(newName))
            return;

        string destinationPath = Path.Combine(archiveBasePath, newName);

        if (Directory.Exists(destinationPath))
        {
            await _dialogService.ShowErrorDialogAsync("Archive Exists",
                $"An archive named '{newName}' already exists.  Please choose a different name.");
            return;
        }

        // Confirm with details
        bool confirmed = await _dialogService.ShowConfirmationDialogAsync(
            "Confirm Archive",
            $"Are you sure you want to archive this folder?\n\n" +
            $"Source: {SelectedFolderToArchive}\n" +
            $"Destination: {destinationPath}\n\n" +
            $"The folder will be MOVED (not copied) to the archive location.");

        if (!confirmed) return;

        IsArchiving = true;
        ArchiveProgressText = $"Moving '{folderName}' to archive...";

        try
        {
            await Task.Run(() => Directory.Move(SelectedFolderToArchive, destinationPath));

            _infoBarService.Show("Archive Complete", $"'{newName}' has been archived successfully.", InfoBarSeverity.Success);
            SelectedFolderToArchive = null;
            await LoadArchivedDirectoriesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Archive Failed", $"Could not archive folder: {ex.Message}");
        }
        finally
        {
            IsArchiving = false;
        }
    }

    [RelayCommand]
    private void ViewArchivedFolder(string folderName)
    {
        if (string.IsNullOrEmpty(folderName)) return;

        AppSettings settings = _settingsService.LoadSettings();
        string archiveBasePath = settings.InventoryArchiveDirectory;

        if (string.IsNullOrEmpty(archiveBasePath))
        {
            _infoBarService.Show("Error", "Archive path not configured.", InfoBarSeverity.Error);
            return;
        }

        string fullPath = Path.Combine(archiveBasePath, folderName);
        _navigationService.NavigateTo(typeof(Views.InventoryViewerPage), fullPath);
    }

    public async Task OpenInExplorerAsync(ArchiveItem item)
    {
        if (item == null) return;
        try
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(item.FullPath);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task RenameArchiveAsync(ArchiveItem item)
    {
        if (item == null) return;

        var (result, newName) = await _dialogService.ShowTextInputDialog(
            "Rename Archive",
            "Enter a new name for this archive:",
            item.Name);

        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(newName) || newName == item.Name)
            return;

        AppSettings settings = _settingsService.LoadSettings();
        string archiveBasePath = settings.InventoryArchiveDirectory;
        string newPath = Path.Combine(archiveBasePath, newName);

        if (Directory.Exists(newPath))
        {
            _infoBarService.Show("Name Exists", $"An archive named '{newName}' already exists.", InfoBarSeverity.Warning);
            return;
        }

        try
        {
            await Task.Run(() => Directory.Move(item.FullPath, newPath));
            _infoBarService.Show("Renamed", $"Archive renamed to '{newName}'.", InfoBarSeverity.Success);
            await LoadArchivedDirectoriesAsync();
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not rename archive: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteArchiveAsync(ArchiveItem item)
    {
        if (item == null) return;

        bool confirmed = await _dialogService.ShowConfirmationDialogAsync(
            "Delete Archive",
            $"Are you sure you want to permanently delete '{item.Name}'?\n\n" +
            $"This will delete {item.FileCount} files and {item.FolderCount} folders.\n" +
            $"This action cannot be undone.");

        if (!confirmed) return;

        try
        {
            await Task.Run(() => Directory.Delete(item.FullPath, true));
            _infoBarService.Show("Deleted", $"'{item.Name}' has been deleted.", InfoBarSeverity.Success);
            await LoadArchivedDirectoriesAsync();
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not delete archive: {ex.Message}", InfoBarSeverity.Error);
        }
    }
}