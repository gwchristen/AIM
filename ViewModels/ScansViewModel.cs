using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class ScansViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IInfoBarService _infoBarService;
    private string _rootScanPath;
    private bool _isPageActive = true;

    private readonly List<ScanTreeItem> _masterItemList = new();
    public ObservableCollection<ScanTreeItem> CurrentItems { get; } = new();
    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = new();

    #region Observable Properties
    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _isListVisible;

    [ObservableProperty]
    private bool _isEmptyMessageVisible;

    [ObservableProperty]
    private bool _isGoUpEnabled;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingText = "Loading... ";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private int _selectedFileCount;

    [ObservableProperty]
    private string _selectedSizeText = "0 B";

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private int _folderCount;

    [ObservableProperty]
    private string _totalSizeText = string.Empty;

    [ObservableProperty]
    private string _folderStatsText = string.Empty;

    [ObservableProperty]
    private bool _isNameSorted = true;

    [ObservableProperty]
    private bool _isDateSorted;

    [ObservableProperty]
    private bool _isSizeSorted;

    [ObservableProperty]
    private string _nameSortIcon = "\uE74A";

    [ObservableProperty]
    private string _dateSortIcon = "\uE74A";

    [ObservableProperty]
    private string _sizeSortIcon = "\uE74A";
    #endregion

    private string _currentSortColumn = "Name";
    private bool _isSortAscending = true;

    public ScansViewModel(MainViewModel mainViewModel, INavigationService navigationService, ISettingsService settingsService, IInfoBarService infoBarService)
    {
        _mainViewModel = mainViewModel;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _infoBarService = infoBarService;

        _mainViewModel.SelectedScanFiles.CollectionChanged += (s, e) => UpdateSelectionStats();
    }

    #region Directory Loading
    private async Task LoadCurrentDirectoryAsync(string directoryPath)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        LoadingText = $"Loading {Path.GetFileName(directoryPath)}... ";
        _masterItemList.Clear();

        try
        {
            await Task.Run(() =>
            {
                var allowedExtensions = new[] { ".txt", ".csv" };

                foreach (var dirPath in Directory.GetDirectories(directoryPath))
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    _masterItemList.Add(new ScanTreeItem
                    {
                        Name = dirInfo.Name,
                        FullPath = dirPath,
                        IsFolder = true,
                        ModifiedDate = dirInfo.LastWriteTime
                    });
                }

                foreach (var filePath in Directory.GetFiles(directoryPath))
                {
                    var ext = Path.GetExtension(filePath).ToLower();
                    if (allowedExtensions.Contains(ext))
                    {
                        var fileInfo = new FileInfo(filePath);
                        _masterItemList.Add(new ScanTreeItem
                        {
                            Name = fileInfo.Name,
                            FullPath = fileInfo.FullName,
                            IsFolder = false,
                            Size = fileInfo.Length,
                            ModifiedDate = fileInfo.LastWriteTime
                        });
                    }
                }
            });

            UpdateBreadcrumbs(directoryPath);
            UpdateFolderStats();
        }
        catch (UnauthorizedAccessException)
        {
            HasError = true;
            ErrorMessage = "Access denied. You don't have permission to view this folder.";
        }
        catch (DirectoryNotFoundException)
        {
            HasError = true;
            ErrorMessage = "Folder not found.  It may have been moved or deleted.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load folder: {ex.Message}";
            _infoBarService.Show("Error", $"Could not read directory: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            ApplyFilterAndSort();
            IsLoading = false;
        }
    }

    private void UpdateBreadcrumbs(string currentPath)
    {
        Breadcrumbs.Clear();
        if (string.IsNullOrEmpty(_rootScanPath)) return;

        var pathSegments = new List<BreadcrumbItem>();
        var relativePath = Path.GetRelativePath(_rootScanPath, currentPath);
        pathSegments.Add(new BreadcrumbItem { Name = "Scans", FullPath = _rootScanPath });

        if (relativePath != ".")
        {
            var currentFullPath = _rootScanPath;
            foreach (var part in relativePath.Split(Path.DirectorySeparatorChar))
            {
                currentFullPath = Path.Combine(currentFullPath, part);
                pathSegments.Add(new BreadcrumbItem { Name = part, FullPath = currentFullPath });
            }
        }

        if (pathSegments.Any()) pathSegments.Last().IsLast = true;
        foreach (var segment in pathSegments) Breadcrumbs.Add(segment);
        IsGoUpEnabled = Breadcrumbs.Count > 1;
    }

    private void UpdateFolderStats()
    {
        FileCount = _masterItemList.Count(i => !i.IsFolder);
        FolderCount = _masterItemList.Count(i => i.IsFolder);

        var totalSize = _masterItemList.Where(i => !i.IsFolder).Sum(i => i.Size);
        TotalSizeText = FormatFileSize(totalSize);

        FolderStatsText = $"{FileCount} files, {FolderCount} folders";
    }

    private void UpdateSelectionStats()
    {
        SelectedFileCount = _mainViewModel.SelectedScanFiles.Count;
        HasSelection = SelectedFileCount > 0;

        if (HasSelection)
        {
            var totalSize = _mainViewModel.SelectedScanFiles.Sum(f => f.Size);
            SelectedSizeText = FormatFileSize(totalSize);
        }
        else
        {
            SelectedSizeText = "0 B";
        }

        foreach (var item in CurrentItems)
        {
            item.IsPersistentlySelected = !item.IsFolder &&
                _mainViewModel.SelectedScanFiles.Any(sf => sf.FullPath == item.FullPath);
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
    #endregion

    #region Filter and Sort
    partial void OnFilterTextChanged(string value) => ApplyFilterAndSort();

    private void ApplyFilterAndSort()
    {
        IEnumerable<ScanTreeItem> processedItems = _masterItemList;

        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            processedItems = processedItems.Where(f => f.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }

        Func<ScanTreeItem, object> keySelector = _currentSortColumn switch
        {
            "Date" => i => i.ModifiedDate,
            "Size" => i => i.Size,
            _ => i => i.Name
        };

        var sortedItems = _isSortAscending
            ? processedItems.OrderBy(i => !i.IsFolder).ThenBy(keySelector)
            : processedItems.OrderBy(i => !i.IsFolder).ThenByDescending(keySelector);

        CurrentItems.Clear();
        foreach (var item in sortedItems)
        {
            item.IsSelected = !item.IsFolder && _mainViewModel.SelectedScanFiles.Any(sf => sf.FullPath == item.FullPath);
            item.IsPersistentlySelected = item.IsSelected;
            CurrentItems.Add(item);
        }

        IsEmptyMessageVisible = CurrentItems.Count == 0 && !HasError;
        IsListVisible = CurrentItems.Count > 0 && !HasError;
    }

    private void UpdateSortIndicators()
    {
        IsNameSorted = _currentSortColumn == "Name";
        IsDateSorted = _currentSortColumn == "Date";
        IsSizeSorted = _currentSortColumn == "Size";

        var icon = _isSortAscending ? "\uE74A" : "\uE74B";
        NameSortIcon = icon;
        DateSortIcon = icon;
        SizeSortIcon = icon;
    }
    #endregion

    #region Commands
    public void DeactivatePage()
    {
        _isPageActive = false;
    }

    [RelayCommand]
    private async Task PageLoaded()
    {
        _isPageActive = true;
        var settings = _settingsService.LoadSettings();
        _rootScanPath = settings.FileScansDirectory;

        if (string.IsNullOrEmpty(_rootScanPath) || !Directory.Exists(_rootScanPath))
        {
            HasError = true;
            ErrorMessage = "The scans directory is not configured or doesn't exist.  Please configure it in Settings. ";
            CurrentItems.Clear();
            Breadcrumbs.Clear();
            return;
        }

        await LoadCurrentDirectoryAsync(_rootScanPath);
    }

    [RelayCommand]
    private async Task Refresh()
    {
        var currentPath = Breadcrumbs.LastOrDefault()?.FullPath ?? _rootScanPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            await PageLoaded();
            return;
        }
        await LoadCurrentDirectoryAsync(currentPath);
    }

    [RelayCommand]
    private void Sort(string newSortColumn)
    {
        if (_currentSortColumn == newSortColumn)
        {
            _isSortAscending = !_isSortAscending;
        }
        else
        {
            _currentSortColumn = newSortColumn;
            _isSortAscending = true;
        }
        UpdateSortIndicators();
        ApplyFilterAndSort();
    }

    [RelayCommand]
    private void SelectionChanged(IList<object> selectedItems)
    {
        if (selectedItems == null) return;

        var selectedPaths = selectedItems.Cast<ScanTreeItem>()
            .Where(i => !i.IsFolder)
            .Select(i => i.FullPath)
            .ToHashSet();

        var visiblePaths = CurrentItems.Where(i => !i.IsFolder).Select(i => i.FullPath).ToHashSet();

        var itemsToRemove = _mainViewModel.SelectedScanFiles
            .Where(sf => visiblePaths.Contains(sf.FullPath) && !selectedPaths.Contains(sf.FullPath))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            _mainViewModel.SelectedScanFiles.Remove(item);
        }

        foreach (var path in selectedPaths)
        {
            if (!_mainViewModel.SelectedScanFiles.Any(sf => sf.FullPath == path))
            {
                var fileItem = CurrentItems.First(i => i.FullPath == path);
                var fileType = Path.GetExtension(fileItem.FullPath).ToLower() == ".csv" ? FileType.Csv : FileType.Text;
                _mainViewModel.SelectedScanFiles.Add(new FileItem
                {
                    Name = fileItem.Name,
                    FullPath = fileItem.FullPath,
                    Type = fileType,
                    Size = fileItem.Size,
                    ModifiedDate = fileItem.ModifiedDate
                });
            }
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in CurrentItems.Where(i => !i.IsFolder))
        {
            if (!_mainViewModel.SelectedScanFiles.Any(sf => sf.FullPath == item.FullPath))
            {
                var fileType = Path.GetExtension(item.FullPath).ToLower() == ".csv" ? FileType.Csv : FileType.Text;
                _mainViewModel.SelectedScanFiles.Add(new FileItem
                {
                    Name = item.Name,
                    FullPath = item.FullPath,
                    Type = fileType,
                    Size = item.Size,
                    ModifiedDate = item.ModifiedDate
                });
            }
            item.IsSelected = true;
            item.IsPersistentlySelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        _mainViewModel.SelectedScanFiles.Clear();
        foreach (var item in CurrentItems)
        {
            item.IsSelected = false;
            item.IsPersistentlySelected = false;
        }
    }

    [RelayCommand]
    private void OpenFile(object selectedItem)
    {
        if (selectedItem is not ScanTreeItem item || item.IsFolder) return;
        var fileItem = new FileItem
        {
            Name = item.Name,
            FullPath = item.FullPath,
            Type = Path.GetExtension(item.FullPath).ToLower() == ".csv" ? FileType.Csv : FileType.Text
        };
        _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
    }

    [RelayCommand]
    private async Task NavigateToFolder(object selectedItem)
    {
        if (selectedItem is not ScanTreeItem item || !item.IsFolder) return;
        await LoadCurrentDirectoryAsync(item.FullPath);
    }

    [RelayCommand]
    private async Task NavigateBreadcrumb(object breadcrumb)
    {
        if (breadcrumb is BreadcrumbItem item)
        {
            await LoadCurrentDirectoryAsync(item.FullPath);
        }
    }

    [RelayCommand]
    private async Task GoUp()
    {
        var parent = Breadcrumbs.ElementAtOrDefault(Breadcrumbs.Count - 2);
        if (parent != null)
        {
            await LoadCurrentDirectoryAsync(parent.FullPath);
        }
    }

    [RelayCommand]
    private void CopyPath(ScanTreeItem item)
    {
        if (item == null) return;
        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(item.FullPath);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        _infoBarService.Show("Copied", "Path copied to clipboard.", InfoBarSeverity.Success, 2000);
    }

    public async Task OpenFileLocationAsync(ScanTreeItem item)
    {
        if (item == null) return;
        try
        {
            var folderPath = item.IsFolder ? item.FullPath : Path.GetDirectoryName(item.FullPath);
            await Windows.System.Launcher.LaunchFolderPathAsync(folderPath);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void Delete(ScanTreeItem item)
    {
        if (item == null) return;
        try
        {
            if (item.IsFolder)
            {
                Directory.Delete(item.FullPath, true);
            }
            else
            {
                File.Delete(item.FullPath);
                var selectedItem = _mainViewModel.SelectedScanFiles.FirstOrDefault(sf => sf.FullPath == item.FullPath);
                if (selectedItem != null)
                {
                    _mainViewModel.SelectedScanFiles.Remove(selectedItem);
                }
            }
            _infoBarService.Show("Deleted", $"'{item.Name}' was deleted.", InfoBarSeverity.Success, 3000);
            RefreshCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not delete '{item.Name}': {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void CreateNewFolder()
    {
        var currentPath = Breadcrumbs.LastOrDefault()?.FullPath;
        if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
        {
            _infoBarService.Show("Error", "Cannot create folder in an invalid directory.", InfoBarSeverity.Error);
            return;
        }

        try
        {
            string newFolderName = "New folder";
            string newFolderPath = Path.Combine(currentPath, newFolderName);
            int counter = 2;

            while (Directory.Exists(newFolderPath))
            {
                newFolderName = $"New folder ({counter++})";
                newFolderPath = Path.Combine(currentPath, newFolderName);
            }

            Directory.CreateDirectory(newFolderPath);
            _infoBarService.Show("Success", $"Folder '{newFolderName}' was created.", InfoBarSeverity.Success, 3000);
            RefreshCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not create folder: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void Rename(Tuple<ScanTreeItem, string> parameters)
    {
        var item = parameters.Item1;
        var newName = parameters.Item2;

        if (item == null || string.IsNullOrWhiteSpace(newName) || item.Name == newName)
        {
            if (item != null) item.IsRenaming = false;
            return;
        }

        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _infoBarService.Show("Invalid Name", "The name contains invalid characters.", InfoBarSeverity.Warning);
            item.IsRenaming = false;
            return;
        }

        var directoryPath = Path.GetDirectoryName(item.FullPath);
        var newFullPath = Path.Combine(directoryPath, newName);

        if (File.Exists(newFullPath) || Directory.Exists(newFullPath))
        {
            _infoBarService.Show("Name Exists", $"An item named '{newName}' already exists.", InfoBarSeverity.Warning);
            item.IsRenaming = false;
            return;
        }

        try
        {
            if (item.IsFolder)
            {
                Directory.Move(item.FullPath, newFullPath);
            }
            else
            {
                File.Move(item.FullPath, newFullPath);

                var selectedItem = _mainViewModel.SelectedScanFiles.FirstOrDefault(sf => sf.FullPath == item.FullPath);
                if (selectedItem != null)
                {
                    selectedItem.Name = newName;
                    selectedItem.FullPath = newFullPath;
                }
            }

            item.Name = newName;
            item.FullPath = newFullPath;
            _infoBarService.Show("Renamed", $"Renamed to '{newName}'.", InfoBarSeverity.Success, 3000);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not rename: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            item.IsRenaming = false;
        }
    }

    [RelayCommand]
    private void AddToBrowseSelection(ScanTreeItem item)
    {
        if (item == null || item.IsFolder) return;

        if (!_mainViewModel.SelectedScanFiles.Any(sf => sf.FullPath == item.FullPath))
        {
            var fileType = Path.GetExtension(item.FullPath).ToLower() == ".csv" ? FileType.Csv : FileType.Text;
            _mainViewModel.SelectedScanFiles.Add(new FileItem
            {
                Name = item.Name,
                FullPath = item.FullPath,
                Type = fileType,
                Size = item.Size,
                ModifiedDate = item.ModifiedDate
            });
            item.IsSelected = true;
            item.IsPersistentlySelected = true;
            _infoBarService.Show("Added", $"'{item.Name}' added to selection.", InfoBarSeverity.Success, 2000);
        }
        else
        {
            _infoBarService.Show("Already Selected", $"'{item.Name}' is already in selection.", InfoBarSeverity.Informational, 2000);
        }
    }

    [RelayCommand]
    private void AddSelectedToBrowse()
    {
        var count = _mainViewModel.SelectedScanFiles.Count;
        if (count > 0)
        {
            _infoBarService.Show("Ready", $"{count} file(s) ready to copy to Browse tab.", InfoBarSeverity.Success, 3000);
        }
    }
    #endregion
}