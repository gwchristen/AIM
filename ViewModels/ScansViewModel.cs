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

    private readonly List<ScanTreeItem> _masterItemList = new();
    public ObservableCollection<ScanTreeItem> CurrentItems { get; } = new();
    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = new();

    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private bool _isListVisible;
    [ObservableProperty] private bool _isEmptyMessageVisible;
    [ObservableProperty] private bool _isGoUpEnabled;
    [ObservableProperty] private bool _isLoading;

    private string _currentSortColumn = "Name";
    private bool _isSortAscending = true;

    public ScansViewModel(MainViewModel mainViewModel, INavigationService navigationService, ISettingsService settingsService, IInfoBarService infoBarService)
    {
        _mainViewModel = mainViewModel;
        _navigationService = navigationService;
        _settingsService = settingsService;
        _infoBarService = infoBarService;
    }

    private async Task LoadCurrentDirectoryAsync(string directoryPath)
    {
        IsLoading = true;
        _masterItemList.Clear();
        try
        {
            await Task.Run(() =>
            {
                var allowedExtensions = new[] { ".txt", ".csv" };
                foreach (var dirPath in Directory.GetDirectories(directoryPath))
                {
                    _masterItemList.Add(new ScanTreeItem { Name = Path.GetFileName(dirPath), FullPath = dirPath, IsFolder = true, ModifiedDate = Directory.GetLastWriteTime(dirPath) });
                }
                foreach (var filePath in Directory.GetFiles(directoryPath))
                {
                    if (allowedExtensions.Contains(Path.GetExtension(filePath).ToLower()))
                    {
                        var fileInfo = new FileInfo(filePath);
                        _masterItemList.Add(new ScanTreeItem { Name = fileInfo.Name, FullPath = fileInfo.FullName, IsFolder = false, Size = fileInfo.Length, ModifiedDate = fileInfo.LastWriteTime });
                    }
                }
            });
            UpdateBreadcrumbs(directoryPath);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not read directory '{Path.GetFileName(directoryPath)}': {ex.Message}", InfoBarSeverity.Error, 0);
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
        if (pathSegments.Any()) { pathSegments.Last().IsLast = true; }
        foreach (var segment in pathSegments) { Breadcrumbs.Add(segment); }
        IsGoUpEnabled = Breadcrumbs.Count > 1;
    }

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
        var sortedItems = _isSortAscending ? processedItems.OrderBy(i => !i.IsFolder).ThenBy(keySelector) : processedItems.OrderBy(i => !i.IsFolder).ThenByDescending(keySelector);
        CurrentItems.Clear();
        foreach (var item in sortedItems)
        {
            item.IsSelected = !item.IsFolder && _mainViewModel.SelectedScanFiles.Any(sf => sf.FullPath == item.FullPath);
            CurrentItems.Add(item);
        }
        IsEmptyMessageVisible = CurrentItems.Count == 0;
        IsListVisible = !IsEmptyMessageVisible;
    }

    [RelayCommand]
    private async Task PageLoaded()
    {
        var settings = _settingsService.LoadSettings();
        _rootScanPath = settings.FileScansDirectory;
        if (string.IsNullOrEmpty(_rootScanPath) || !Directory.Exists(_rootScanPath))
        {
            _infoBarService.Show("Configuration needed", "The directory for scans could not be found. Please configure it in Settings.", InfoBarSeverity.Warning, 0);
            CurrentItems.Clear(); Breadcrumbs.Clear(); return;
        }
        await LoadCurrentDirectoryAsync(_rootScanPath);
    }

    [RelayCommand]
    private async Task Refresh()
    {
        var currentPath = Breadcrumbs.LastOrDefault()?.FullPath ?? _rootScanPath;
        if (string.IsNullOrEmpty(currentPath)) { await PageLoaded(); return; }
        _infoBarService.Show("Refreshing...", "Loading contents of the current directory.", InfoBarSeverity.Informational, 3000);
        await LoadCurrentDirectoryAsync(currentPath);
    }

    [RelayCommand]
    private void Sort(string newSortColumn)
    {
        if (_currentSortColumn == newSortColumn) _isSortAscending = !_isSortAscending;
        else { _currentSortColumn = newSortColumn; _isSortAscending = true; }
        ApplyFilterAndSort();
    }

    [RelayCommand]
    private void SelectionChanged(IList<object> selectedItems)
    {
        if (selectedItems == null) return;
        var selectedPaths = selectedItems.Cast<ScanTreeItem>().Where(i => !i.IsFolder).Select(i => i.FullPath).ToHashSet();
        var visiblePaths = CurrentItems.Where(i => !i.IsFolder).Select(i => i.FullPath).ToHashSet();
        var itemsToRemove = _mainViewModel.SelectedScanFiles.Where(sf => visiblePaths.Contains(sf.FullPath) && !selectedPaths.Contains(sf.FullPath)).ToList();
        foreach (var item in itemsToRemove) { _mainViewModel.SelectedScanFiles.Remove(item); }
        foreach (var path in selectedPaths)
        {
            if (!_mainViewModel.SelectedScanFiles.Any(sf => sf.FullPath == path))
            {
                var fileItem = CurrentItems.First(i => i.FullPath == path);
                var fileType = Path.GetExtension(fileItem.FullPath).Trim('.').ToUpper() == "CSV" ? FileType.Csv : FileType.Text;
                _mainViewModel.SelectedScanFiles.Add(new FileItem { Name = fileItem.Name, FullPath = fileItem.FullPath, Type = fileType, Size = fileItem.Size, ModifiedDate = fileItem.ModifiedDate });
            }
        }
    }

    [RelayCommand]
    private void OpenFile(object selectedItem)
    {
        if (selectedItem is not ScanTreeItem item || item.IsFolder) return;
        var fileItem = new FileItem { Name = item.Name, FullPath = item.FullPath };
        _navigationService.NavigateTo(typeof(PreviewPage), fileItem, "Preview");
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
        if (breadcrumb is BreadcrumbItem item) { await LoadCurrentDirectoryAsync(item.FullPath); }
    }

    [RelayCommand]
    private async Task GoUp()
    {
        var parent = Breadcrumbs.ElementAtOrDefault(Breadcrumbs.Count - 2);
        if (parent != null) { await LoadCurrentDirectoryAsync(parent.FullPath); }
    }

    [RelayCommand]
    private async Task CopyPath(ScanTreeItem item)
    {
        if (item == null) return;
        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(item.FullPath);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        _infoBarService.Show("Copied", "Path copied to clipboard.", InfoBarSeverity.Success, 2000);
    }

    [RelayCommand] private async Task OpenFolder(ScanTreeItem item) => await NavigateToFolder(item);

    [RelayCommand]
    private void Delete(ScanTreeItem item)
    {
        if (item == null) return;
        try
        {
            if (item.IsFolder) { Directory.Delete(item.FullPath, true); } else { File.Delete(item.FullPath); }
            _infoBarService.Show("Deleted", $"'{item.Name}' was successfully deleted.", InfoBarSeverity.Success, 3000);
            RefreshCommand.Execute(null);
        }
        catch (Exception ex) { _infoBarService.Show("Error", $"Could not delete '{item.Name}': {ex.Message}", InfoBarSeverity.Error, 5000); }
    }

    [RelayCommand]
    private void CreateNewFolder()
    {
        var currentPath = Breadcrumbs.LastOrDefault()?.FullPath;
        if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
        {
            _infoBarService.Show("Error", "Cannot create folder in an invalid directory.", InfoBarSeverity.Error); return;
        }
        try
        {
            string newFolderName = "New folder", newFolderPath = Path.Combine(currentPath, newFolderName);
            int counter = 2;
            while (Directory.Exists(newFolderPath))
            {
                newFolderName = $"New folder ({counter++})"; newFolderPath = Path.Combine(currentPath, newFolderName);
            }
            Directory.CreateDirectory(newFolderPath);
            _infoBarService.Show("Success", $"Folder '{newFolderName}' was created.", InfoBarSeverity.Success, 3000);
            RefreshCommand.Execute(null);
        }
        catch (Exception ex) { _infoBarService.Show("Error", $"Could not create new folder: {ex.Message}", InfoBarSeverity.Error, 5000); }
    }

    [RelayCommand]
    private void Rename(Tuple<ScanTreeItem, string> parameters)
    {
        var item = parameters.Item1;
        var newName = parameters.Item2;
        if (item == null || string.IsNullOrWhiteSpace(newName) || item.Name == newName)
        {
            if (item != null) item.IsRenaming = false; return;
        }
        var directoryPath = Path.GetDirectoryName(item.FullPath);
        var newFullPath = Path.Combine(directoryPath, newName);
        try
        {
            if (item.IsFolder) { Directory.Move(item.FullPath, newFullPath); } else { File.Move(item.FullPath, newFullPath); }
            item.Name = newName; item.FullPath = newFullPath;
            _infoBarService.Show("Renamed", $"Item was successfully renamed to '{newName}'.", InfoBarSeverity.Success, 3000);
        }
        catch (Exception ex) { _infoBarService.Show("Error", $"Could not rename item: {ex.Message}", InfoBarSeverity.Error, 5000); }
        finally { item.IsRenaming = false; }
    }
}