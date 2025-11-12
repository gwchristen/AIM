using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Controls; // Required for InfoBarSeverity

namespace AIM.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly INavigationService _navigationService;
    private readonly MainViewModel _mainViewModel;
    private readonly IInfoBarService _infoBarService; // NEW: The info bar service

    [ObservableProperty]
    private string searchDirectory = string.Empty;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool isSearching = false;

    // --- DELETED: The old StatusMessage and IsStatusMessageVisible properties are no longer needed. ---

    [ObservableProperty]
    private bool isContentSearch = true;

    public ObservableCollection<FileItem> SearchResults { get; } = new();

    // UPDATED: The constructor now asks for IInfoBarService
    public SearchViewModel(ISearchService searchService, INavigationService navigationService, MainViewModel mainViewModel, IInfoBarService infoBarService)
    {
        _searchService = searchService;
        _navigationService = navigationService;
        _mainViewModel = mainViewModel;
        _infoBarService = infoBarService; // NEW: Store the service instance
        SearchDirectory = _mainViewModel.SelectedRoot;
    }

    [RelayCommand]
    private void HandleItemDoubleClick(object selectedItem)
    {
        if (selectedItem is FileItem fileItem)
        {
            _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
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
        // NEW: Show an informational message
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
                // NEW: Show a warning message that does not auto-hide
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

            // NEW: Show a success message
            _infoBarService.Show("Success", $"Search complete. Found {SearchResults.Count} files.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            // NEW: Show an error message that does not auto-hide
            _infoBarService.Show("Error", $"An error occurred during search: {ex.Message}", InfoBarSeverity.Error, 0);
        }
        finally
        {
            IsSearching = false;
        }
    }
}