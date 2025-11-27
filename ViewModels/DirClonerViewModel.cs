using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
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
    #endregion

    public ObservableCollection<string> SourcePreviewFolders { get; } = new();

    public bool CanClone => !string.IsNullOrEmpty(SourceDirectory) && !string.IsNullOrEmpty(DestinationDirectory);
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

    private bool CanCreateStructure() => !string.IsNullOrEmpty(SourceDirectory) && !string.IsNullOrEmpty(DestinationDirectory);

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
                SourcePreviewMore = $"... and {folders.Count - 8} more folders";
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

        if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary || string.IsNullOrWhiteSpace(newName))
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

        try
        {
            int folderCount = 0;

            await Task.Run(() =>
            {
                folderCount = CloneDirectoryStructure(SourceDirectory, targetPath);
            });

            ResultPath = targetPath;
            ResultSummary = $"Successfully created {folderCount} folders at:\n{targetPath}";
            ShowResults = true;

            _infoBarService.Show("Clone Complete", $"Created {folderCount} folders.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Clone Failed", $"Could not clone directory structure: {ex.Message}");
        }
        finally
        {
            IsCloning = false;
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
            await Windows.System.Launcher.LaunchFolderPathAsync(ResultPath);
        }
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
}