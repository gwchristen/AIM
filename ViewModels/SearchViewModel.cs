using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly INavigationService _navigationService;
    private readonly MainViewModel _mainViewModel;
    private readonly IInfoBarService _infoBarService;
    private CancellationTokenSource _searchCancellationToken;
    private Stopwatch _searchStopwatch;

    #region Observable Properties
    [ObservableProperty]
    private string _searchDirectory = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private bool _isContentSearch = true;

    [ObservableProperty]
    private int _searchTypeIndex = 0;

    [ObservableProperty]
    private int _fileTypeIndex = 0;

    [ObservableProperty]
    private bool _isCaseSensitive = false;

    [ObservableProperty]
    private bool _useWildcards = false;

    [ObservableProperty]
    private int _dateFilterIndex = 0;

    [ObservableProperty]
    private int _sortIndex = 0;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private string _searchProgressText = "Searching...";

    [ObservableProperty]
    private int _resultCount = 0;

    [ObservableProperty]
    private string _searchTimeText = string.Empty;

    [ObservableProperty]
    private bool _hasResults = false;

    [ObservableProperty]
    private bool _showNoResults = false;

    [ObservableProperty]
    private bool _showEmptyState = true;

    [ObservableProperty]
    private bool _hasSearchHistory = false;

    [ObservableProperty]
    private int _selectedCount = 0;

    [ObservableProperty]
    private bool _hasSelection = false;
    #endregion

    #region Collections
    public ObservableCollection<SearchResultItem> SearchResults { get; } = new();
    public ObservableCollection<string> SearchHistory { get; } = new();
    #endregion

    #region Computed Properties
    public bool CanSearch => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching;

    public bool QueryHasWildcards => SearchQuery.Contains('*') || SearchQuery.Contains('?');
    #endregion

    public SearchViewModel(ISearchService searchService, INavigationService navigationService, MainViewModel mainViewModel, IInfoBarService infoBarService, IRefreshService refreshService)
    {
        _searchService = searchService;
        _navigationService = navigationService;
        _mainViewModel = mainViewModel;
        _infoBarService = infoBarService;
        SearchDirectory = _mainViewModel.SelectedRoot;
        refreshService.RefreshRequested += (s, e) =>
        {
            if (!string.IsNullOrEmpty(SearchQuery) && HasResults)
            {
                SearchCommand.Execute(null);
            }
        };

        LoadSearchHistory();
    }

    #region Property Changed Handlers
    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(CanSearch));
    }

    partial void OnIsSearchingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSearch));
    }

    partial void OnSortIndexChanged(int value)
    {
        SortResults();
    }
    #endregion

    #region Search History
    private void LoadSearchHistory()
    {
        HasSearchHistory = SearchHistory.Count > 0;
    }

    private void AddToHistory(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        if (SearchHistory.Contains(query))
        {
            SearchHistory.Remove(query);
        }

        SearchHistory.Insert(0, query);

        while (SearchHistory.Count > 20)
        {
            SearchHistory.RemoveAt(SearchHistory.Count - 1);
        }

        HasSearchHistory = SearchHistory.Count > 0;
        OnPropertyChanged(nameof(SearchHistory));
    }

    [RelayCommand]
    private void ClearHistory()
    {
        SearchHistory.Clear();
        HasSearchHistory = false;
    }
    #endregion

    #region Commands
    [RelayCommand]
    private async Task Browse()
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");
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

        _searchCancellationToken = new CancellationTokenSource();
        _searchStopwatch = Stopwatch.StartNew();

        IsSearching = true;
        ShowEmptyState = false;
        ShowNoResults = false;
        HasResults = false;
        SearchResults.Clear();
        SearchProgressText = "Initializing search...";

        try
        {
            var rootPath = string.IsNullOrEmpty(SearchDirectory) ? _mainViewModel.SelectedRoot : SearchDirectory;

            if (string.IsNullOrEmpty(rootPath))
            {
                _infoBarService.Show("No Directory", "Please select a search directory or set the Root Directory in Settings.", InfoBarSeverity.Warning);
                IsSearching = false;
                ShowEmptyState = true;
                return;
            }

            if (!Directory.Exists(rootPath))
            {
                _infoBarService.Show("Directory Not Found", $"The directory '{rootPath}' does not exist.", InfoBarSeverity.Error);
                IsSearching = false;
                ShowEmptyState = true;
                return;
            }

            AddToHistory(SearchQuery);

            var options = new SearchOptions
            {
                Query = SearchQuery,
                RootPath = rootPath,
                SearchType = (SearchType)SearchTypeIndex,
                FileTypeFilter = (FileTypeFilter)FileTypeIndex,
                IsCaseSensitive = IsCaseSensitive,
                UseWildcards = QueryHasWildcards,  // Auto-detect from query
                DateFilter = GetDateFilter(),
                CancellationToken = _searchCancellationToken.Token
            };

            var progress = new Progress<SearchProgress>(p =>
            {
                SearchProgressText = $"Searching...  {p.FilesSearched} files scanned, {p.MatchesFound} matches found";
            });

            var results = await _searchService.SearchAsync(options, progress);

            if (_searchCancellationToken.Token.IsCancellationRequested)
            {
                _infoBarService.Show("Cancelled", "Search was cancelled.", InfoBarSeverity.Informational);
                ShowEmptyState = true;
                return;
            }

            _searchStopwatch.Stop();

            foreach (var item in results)
            {
                SearchResults.Add(item);
            }

            ResultCount = SearchResults.Count;
            SearchTimeText = $"({_searchStopwatch.ElapsedMilliseconds}ms)";

            HasResults = SearchResults.Count > 0;
            ShowNoResults = SearchResults.Count == 0;

            if (HasResults)
            {
                SortResults();
                var modeText = UseWildcards ? " (wildcards)" : "";
                modeText += IsCaseSensitive ? " (exact case)" : "";
                _infoBarService.Show("Search Complete", $"Found {ResultCount} result(s) in {_searchStopwatch.ElapsedMilliseconds}ms{modeText}.", InfoBarSeverity.Success);
            }
        }
        catch (OperationCanceledException)
        {
            _infoBarService.Show("Cancelled", "Search was cancelled.", InfoBarSeverity.Informational);
            ShowEmptyState = true;
        }
        catch (UnauthorizedAccessException)
        {
            _infoBarService.Show("Access Denied", "You don't have permission to search some folders.", InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Search Error", $"An error occurred: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            IsSearching = false;
            _searchCancellationToken?.Dispose();
            _searchCancellationToken = null;
        }
    }

    [RelayCommand]
    private void CancelSearch()
    {
        _searchCancellationToken?.Cancel();
    }

    [RelayCommand]
    private void Clear()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        ResultCount = 0;
        HasResults = false;
        ShowNoResults = false;
        ShowEmptyState = true;
        SearchTimeText = string.Empty;
    }

    [RelayCommand]
    private void HandleItemDoubleClick(object selectedItem)
    {
        if (selectedItem is SearchResultItem item)
        {
            PreviewFile(item);
        }
    }

    [RelayCommand]
    private void PreviewFile(SearchResultItem item)
    {
        if (item != null)
        {
            var fileItem = new FileItem
            {
                Name = item.Name,
                FullPath = item.FullPath,
                Type = item.FileType
            };
            _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
        }
    }

    [RelayCommand]
    private void OpenInBrowse(SearchResultItem item)
    {
        if (item != null)
        {
            var directoryPath = Path.GetDirectoryName(item.FullPath);

            if (App.Current is App app)
            {
                var browseViewModel = app.Services.GetService(typeof(BrowseViewModel)) as BrowseViewModel;
                if (browseViewModel != null)
                {
                    browseViewModel.SelectedLeftDirectory = new DirectoryItem
                    {
                        FullPath = directoryPath,
                        Name = Path.GetFileName(directoryPath)
                    };
                    browseViewModel.SelectedRightDirectory = new DirectoryItem
                    {
                        FullPath = directoryPath,
                        Name = Path.GetFileName(directoryPath)
                    };
                }
            }

            _navigationService.NavigateTo(typeof(BrowsePage));
        }
    }

    [RelayCommand]
    private async Task OpenFileLocation(SearchResultItem item)
    {
        if (item != null)
        {
            var directoryPath = Path.GetDirectoryName(item.FullPath);
            try
            {
                await Windows.System.Launcher.LaunchFolderPathAsync(directoryPath);
            }
            catch (Exception ex)
            {
                _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
            }
        }
    }

    [RelayCommand]
    private void CopyFilePath(SearchResultItem item)
    {
        if (item != null)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(item.FullPath);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            _infoBarService.Show("Copied", "File path copied to clipboard.", InfoBarSeverity.Success, 2000);
        }
    }

    [RelayCommand]
    private void CopyDirectoryPath(SearchResultItem item)
    {
        if (item != null)
        {
            var directoryPath = Path.GetDirectoryName(item.FullPath);
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(directoryPath);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            _infoBarService.Show("Copied", "Directory path copied to clipboard.", InfoBarSeverity.Success, 2000);
        }
    }

    [RelayCommand]
    private void ToggleSortDirection(bool ascending)
    {
        SortAscending = ascending;
        SortResults();
    }
    #endregion

    #region Helper Methods
    private void SortResults()
    {
        if (SearchResults.Count == 0) return;

        var sorted = SortIndex switch
        {
            0 => SortAscending
                ? SearchResults.OrderBy(r => r.Name).ToList()
                : SearchResults.OrderByDescending(r => r.Name).ToList(),
            1 => SortAscending
                ? SearchResults.OrderBy(r => r.ModifiedDate).ToList()
                : SearchResults.OrderByDescending(r => r.ModifiedDate).ToList(),
            2 => SortAscending
                ? SearchResults.OrderBy(r => r.FileSize).ToList()
                : SearchResults.OrderByDescending(r => r.FileSize).ToList(),
            3 => SortAscending
                ? SearchResults.OrderBy(r => r.DirectoryPath).ToList()
                : SearchResults.OrderByDescending(r => r.DirectoryPath).ToList(),
            _ => SearchResults.ToList()
        };

        SearchResults.Clear();
        foreach (var item in sorted)
        {
            SearchResults.Add(item);
        }
    }

    private DateTime? GetDateFilter()
    {
        return DateFilterIndex switch
        {
            1 => DateTime.Today,
            2 => DateTime.Today.AddDays(-7),
            3 => DateTime.Today.AddDays(-30),
            4 => DateTime.Today.AddDays(-90),
            _ => null
        };
    }

    public void UpdateSelection(int count)
    {
        SelectedCount = count;
        HasSelection = count > 0;
    }
    #endregion
}