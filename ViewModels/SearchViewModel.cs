using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Controls;

namespace AIM.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly INavigationService _navigationService;
    private readonly MainViewModel _mainViewModel;
    private readonly IInfoBarService _infoBarService;
    private readonly ISearchStateService _searchStateService;  // NEW
    private readonly IAuditLoggingService _auditLoggingService;

    [ObservableProperty]
    private string searchDirectory = string.Empty;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool isSearching = false;

    [ObservableProperty]
    private bool isContentSearch = true;

    public ObservableCollection<FileItem> SearchResults { get; } = new();

    // UPDATED: Constructor now includes ISearchStateService
    public SearchViewModel(ISearchService searchService, INavigationService navigationService, MainViewModel mainViewModel, IInfoBarService infoBarService, ISearchStateService searchStateService, IAuditLoggingService auditLoggingService)
    {
        _searchService = searchService;
        _navigationService = navigationService;
        _mainViewModel = mainViewModel;
        _infoBarService = infoBarService;
        _searchStateService = searchStateService;  // NEW
        _auditLoggingService = auditLoggingService;
        SearchDirectory = _mainViewModel.SelectedRoot;

        // NEW: Load saved search state when ViewModel is created
        LoadPreviousSearchState();
    }

    // NEW: Load previous search state AND RESULTS
    private void LoadPreviousSearchState()
    {
        var savedState = _searchStateService.LoadSearchState();
        if (savedState != null)
        {
            SearchQuery = savedState.SearchQuery;
            SearchDirectory = savedState.SearchDirectory;
            IsContentSearch = savedState.IsContentSearch;

            // NEW: Restore search results
            SearchResults.Clear();
            foreach (var result in savedState.SearchResults)
            {
                SearchResults.Add(result);
            }

            System.Diagnostics.Debug.WriteLine($"[SearchViewModel] Previous search state restored: {SearchQuery} ({SearchResults.Count} results)");
        }
    }

    // UPDATED: Save search state with results
    private void SaveCurrentSearchState()
    {
        _searchStateService.SaveSearchState(SearchQuery, SearchDirectory, IsContentSearch, SearchResults);
    }

    [RelayCommand]
    public void Preview(FileItem? fileItem)
    {
        if (fileItem == null) return;
        SaveCurrentSearchState();  // NEW: Save state before navigating
        _navigationService.NavigateTo(typeof(PreviewPage), fileItem, "Preview");
    }

    [RelayCommand]
    private void OpenInBrowse(FileItem fileItem)
    {
        if (fileItem == null) return;

        try
        {
            var directoryPath = Path.GetDirectoryName(fileItem.FullPath);
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                _infoBarService.Show("Error", "Directory not found.", InfoBarSeverity.Error);
                return;
            }

            SaveCurrentSearchState();  // NEW: Save state before navigating
            _navigationService.NavigateTo(typeof(BrowsePage), directoryPath, "Browse");
            _infoBarService.Show("Success", $"Opened in Browse: {directoryPath}", InfoBarSeverity.Success, 2000);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not open in browse: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void CopyFilePath(FileItem fileItem)
    {
        if (fileItem == null) return;

        try
        {
            var directoryPath = Path.GetDirectoryName(fileItem.FullPath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                _infoBarService.Show("Error", "Could not determine directory path.", InfoBarSeverity.Error);
                return;
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(directoryPath);
            Clipboard.SetContent(dataPackage);

            _infoBarService.Show("Success", "Directory path copied to clipboard.", InfoBarSeverity.Success, 2000);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not copy directory path: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void HandleItemDoubleClick(object selectedItem)
    {
        if (selectedItem is FileItem fileItem)
        {
            SaveCurrentSearchState();  // NEW: Save state before navigating
            _navigationService.NavigateTo(typeof(PreviewPage), fileItem, "Preview");
        }
    }

    [RelayCommand]
    private async Task Browse()
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            SearchDirectory = folder.Path;
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        SearchResults.Clear();
        _infoBarService.Show("Searching...", $"Searching for '{SearchQuery}'.", InfoBarSeverity.Informational, 3000);

        try
        {
            var rootPath = SearchDirectory;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = _mainViewModel.SelectedRoot;
            }
            if (string.IsNullOrEmpty(rootPath))
            {
                _infoBarService.Show("Warning", "No search directory selected. Please set the Root Directory in Settings.", InfoBarSeverity.Warning, 0);
                return;
            }

            var results = IsContentSearch
                ? await _searchService.SearchContentAsync(SearchQuery, rootPath)
                : await _searchService.SearchFilesAsync(SearchQuery, rootPath);

            foreach (var item in results)
            {
                SearchResults.Add(item);
            }

            SaveCurrentSearchState();  // NEW: Save state with results
            _infoBarService.Show("Success", $"Search complete. Found {SearchResults.Count} files.", InfoBarSeverity.Success);
            
            _auditLoggingService.LogAudit(
                "SEARCH_PERFORMED",
                rootPath,
                $"Search completed: '{SearchQuery}' ({(IsContentSearch ? "content" : "filename")} search) - {SearchResults.Count} results",
                new System.Collections.Generic.Dictionary<string, string>
                {
                    { "query", SearchQuery },
                    { "searchType", IsContentSearch ? "content" : "filename" },
                    { "resultCount", SearchResults.Count.ToString() }
                }
            );
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"An error occurred during search: {ex.Message}", InfoBarSeverity.Error, 0);
            
            _auditLoggingService.LogAudit(
                "SEARCH_FAILED",
                SearchDirectory,
                $"Search failed for '{SearchQuery}': {ex.Message}",
                new System.Collections.Generic.Dictionary<string, string>
                {
                    { "query", SearchQuery },
                    { "error", ex.Message }
                }
            );
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        _searchStateService.ClearSearchState();
        _infoBarService.Show("Cleared", "Search terms and results have been cleared.", InfoBarSeverity.Success, 2000);
    }
}