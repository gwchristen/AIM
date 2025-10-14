#pragma warning disable MVVMTK0045
using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isContentSearch = false;

    public ObservableCollection<FileItem> SearchResults { get; } = new();

    public SearchViewModel()
    {
        _searchService = Ioc.Default.GetService<ISearchService>();
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrEmpty(SearchQuery)) return;

        var rootPath = MainWindow.Instance?.ViewModel?.SelectedRootDirectory?.FullPath ?? string.Empty;
        Debug.WriteLine($"Searching for '{SearchQuery}' in '{rootPath}', Content: {IsContentSearch}");
        if (string.IsNullOrEmpty(rootPath))
        {
            StatusMessage = "Please select a root directory first.";
            return;
        }

        StatusMessage = IsContentSearch ? "Searching file contents..." : "Searching file names...";
        SearchResults.Clear();
        var results = IsContentSearch
            ? await _searchService.SearchContentAsync(SearchQuery, rootPath)
            : await _searchService.SearchFilesAsync(SearchQuery, rootPath);
        foreach (var item in results)
        {
            SearchResults.Add(item);
        }
        StatusMessage = $"Found {SearchResults.Count} results.";
        Debug.WriteLine($"Found {SearchResults.Count} results");
    }
}