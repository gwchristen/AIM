using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI;

namespace AIM.ViewModels;

public partial class DirClonerViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;
    private readonly IInfoBarService _infoBarService;

    #region Observable Properties
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateStructureCommand))]
    [NotifyPropertyChangedFor(nameof(CanClone))]
    [NotifyPropertyChangedFor(nameof(HasSourcePreview))]
    [NotifyPropertyChangedFor(nameof(Step2Color))]
    [NotifyPropertyChangedFor(nameof(Step3Color))]
    private string _sourceDirectory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateStructureCommand))]
    [NotifyPropertyChangedFor(nameof(CanClone))]
    [NotifyPropertyChangedFor(nameof(Step3Color))]
    private string _destinationDirectory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanClone))]
    private bool _isCloning;

    [ObservableProperty]
    private string _cloneProgressText = "Cloning...";

    [ObservableProperty]
    private bool _showResults;

    [ObservableProperty]
    private string _resultSummary;

    [ObservableProperty]
    private string _resultPath;

    [ObservableProperty]
    private bool _hasMoreFolders;

    [ObservableProperty]
    private string _sourcePreviewMore;

    [ObservableProperty]
    private int _foldersCreated;

    [ObservableProperty]
    private string _elapsedTime;
    #endregion

    public ObservableCollection<string> SourcePreviewFolders { get; } = new();

    public bool CanClone => !string.IsNullOrEmpty(SourceDirectory) &&
                            !string.IsNullOrEmpty(DestinationDirectory) &&
                            !IsCloning;

    public bool HasSourcePreview => !string.IsNullOrEmpty(SourceDirectory) && SourcePreviewFolders.Count > 0;

    public SolidColorBrush Step2Color => !string.IsNullOrEmpty(SourceDirectory)
        ? new SolidColorBrush(Color.FromArgb(255, 16, 124, 16))
        : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));

    public SolidColorBrush Step3Color => CanClone
        ? new SolidColorBrush(Color.FromArgb(255, 16, 124, 16))
        : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));

    public DirClonerViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService, IInfoBarService infoBarService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
        _infoBarService = infoBarService;
    }

    private bool CanCreateStructure() => !string.IsNullOrEmpty(SourceDirectory) &&
                                          !string.IsNullOrEmpty(DestinationDirectory) &&
                                          !IsCloning;

    [RelayCommand]
    private async Task SelectSourceAsync()
    {
        var path = await PickFolderAsync();
        if (path != null)
        {
            SourceDirectory = path;
            await LoadSourcePreviewAsync();
        }
    }

    [RelayCommand]
    private async Task SelectDestinationAsync()
    {
        DestinationDirectory = await PickFolderAsync();
    }

    private async Task LoadSourcePreviewAsync()
    {
        SourcePreviewFolders.Clear();
        HasMoreFolders = false;

        if (string.IsNullOrEmpty(SourceDirectory) || !Directory.Exists(SourceDirectory))
            return;

        try
        {
            var folders = await Task.Run(() =>
                Directory.GetDirectories(SourceDirectory)
                    .Select(Path.GetFileName)
                    .OrderBy(n => n)
                    .ToList());

            var displayFolders = folders.Take(8).ToList();
            foreach (var folder in displayFolders)
            {
                SourcePreviewFolders.Add(folder);
            }

            if (folders.Count > 8)
            {
                HasMoreFolders = true;
                SourcePreviewMore = $"...  and {folders.Count - 8} more folders";
            }
        }
        catch { }

        OnPropertyChanged(nameof(HasSourcePreview));
    }

    [RelayCommand(CanExecute = nameof(CanCreateStructure))]
    private async Task CreateStructureAsync()
    {
        var (result, newName) = await _dialogService.ShowTextInputDialog(
            "Name New Directory",
            $"Enter a name for the new directory structure:\n\n" +
            $"Source: {Path.GetFileName(SourceDirectory)}\n" +
            $"Destination: {DestinationDirectory}",
            Path.GetFileName(SourceDirectory) + "_Clone");

        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(newName))
            return;

        string targetPath = Path.Combine(DestinationDirectory, newName);

        if (Directory.Exists(targetPath))
        {
            await _dialogService.ShowErrorDialogAsync("Folder Exists",
                $"A folder named '{newName}' already exists at the destination.");
            return;
        }

        IsCloning = true;
        ShowResults = false;
        CloneProgressText = "Creating directory structure...";
        OnPropertyChanged(nameof(CanClone));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            int folderCount = 0;

            await Task.Run(() =>
            {
                folderCount = CloneDirectoryStructure(SourceDirectory, targetPath);
            });

            stopwatch.Stop();

            ResultPath = targetPath;
            FoldersCreated = folderCount;
            ElapsedTime = FormatElapsedTime(stopwatch.ElapsedMilliseconds);
            ResultSummary = $"Successfully created {folderCount} folders in {ElapsedTime}.\nLocation: {targetPath}";
            ShowResults = true;

            // Show prominent InfoBar notification
            _infoBarService.Show(
                "Clone Complete!",
                $"Successfully created {folderCount} folders in {ElapsedTime}.",
                InfoBarSeverity.Success,
                5000); // Show for 5 seconds
        }
        catch (UnauthorizedAccessException)
        {
            _infoBarService.Show("Access Denied",
                "You don't have permission to create folders at the destination.",
                InfoBarSeverity.Error);
            await _dialogService.ShowErrorDialogAsync("Access Denied",
                "You don't have permission to create folders at the selected destination.  Please choose a different location or run the application as administrator.");
        }
        catch (IOException ex)
        {
            _infoBarService.Show("Clone Failed", ex.Message, InfoBarSeverity.Error);
            await _dialogService.ShowErrorDialogAsync("Clone Failed",
                $"Could not clone directory structure: {ex.Message}");
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Clone Failed", ex.Message, InfoBarSeverity.Error);
            await _dialogService.ShowErrorDialogAsync("Clone Failed",
                $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsCloning = false;
            OnPropertyChanged(nameof(CanClone));
        }
    }

    private int CloneDirectoryStructure(string sourcePath, string destPath)
    {
        int count = 0;
        Directory.CreateDirectory(destPath);
        count++;

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            string dirName = Path.GetFileName(dir);
            string newPath = Path.Combine(destPath, dirName);
            count += CloneDirectoryStructure(dir, newPath);
        }

        return count;
    }

    [RelayCommand]
    private async Task OpenResultFolderAsync()
    {
        if (!string.IsNullOrEmpty(ResultPath) && Directory.Exists(ResultPath))
        {
            try
            {
                await Windows.System.Launcher.LaunchFolderPathAsync(ResultPath);
            }
            catch (Exception ex)
            {
                _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
            }
        }
    }

    [RelayCommand]
    private void DismissResults()
    {
        ShowResults = false;
    }

    [RelayCommand]
    private void Reset()
    {
        SourceDirectory = null;
        DestinationDirectory = null;
        SourcePreviewFolders.Clear();
        HasMoreFolders = false;
        ShowResults = false;
        ResultPath = null;
        ResultSummary = null;
    }

    private async Task<string> PickFolderAsync()
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

    private static string FormatElapsedTime(long milliseconds)
    {
        if (milliseconds < 1000)
            return $"{milliseconds}ms";
        if (milliseconds < 60000)
            return $"{milliseconds / 1000.0:F1}s";
        return $"{milliseconds / 60000.0:F1}m";
    }
}