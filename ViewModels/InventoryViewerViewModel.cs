using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class InventoryViewerViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private string? _archivePath;

    [ObservableProperty]
    private ObservableCollection<TreeViewNode> _rootNodes;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadArchiveCommand))]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        FilterTree(value);
    }

    public InventoryViewerViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _rootNodes = new ObservableCollection<TreeViewNode>();
    }

    [RelayCommand]
    private Task LoadArchiveAsync(string path)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            IsLoading = true;
            RootNodes.Clear();
            ArchivePath = path;
        });

        return Task.Run(() =>
        {
            try
            {
                var rootDirectoryInfo = new DirectoryInfo(path);
                if (!rootDirectoryInfo.Exists) return;

                var rootNode = new TreeViewNode { Content = rootDirectoryInfo.Name, IsExpanded = true };
                LoadSubdirectoriesAndFiles(rootDirectoryInfo, rootNode);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    RootNodes.Add(rootNode);
                    FilterTree(SearchQuery);
                });
            }
            finally
            {
                _dispatcherQueue.TryEnqueue(() => IsLoading = false);
            }
        });
    }

    private void LoadSubdirectoriesAndFiles(DirectoryInfo directoryInfo, TreeViewNode parentNode)
    {
        foreach (var dir in directoryInfo.GetDirectories())
        {
            var dirNode = new TreeViewNode { Content = dir.Name, IsExpanded = true };
            parentNode.Children.Add(dirNode);
            LoadSubdirectoriesAndFiles(dir, dirNode);
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            var fileNode = new TreeViewNode { Content = file.Name, IsExpanded = true };
            parentNode.Children.Add(fileNode);
        }
    }

    private void FilterTree(string? query)
    {
        if (RootNodes.Count == 0) return;

        foreach (var node in RootNodes)
        {
            FilterNode(node, query);
        }
    }

    private bool FilterNode(TreeViewNode node, string? query)
    {
        bool isVisible = false;

        if (string.IsNullOrEmpty(query))
        {
            isVisible = true;
        }
        else
        {
            bool selfMatches = node.Content.Contains(query, StringComparison.OrdinalIgnoreCase);
            bool childrenMatch = false;
            foreach (var child in node.Children)
            {
                if (FilterNode(child, query))
                {
                    childrenMatch = true;
                }
            }
            isVisible = selfMatches || childrenMatch;
        }

        node.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        return isVisible;
    }

    [RelayCommand]
    private void GoBack()
    {
        SearchQuery = string.Empty;
        // Navigate to the Admin Tools page
        _navigationService.NavigateTo(typeof(InventoryAdminToolsPage));
    }
}

public class TreeViewNode : ObservableObject
{
    public string Content { get; set; } = string.Empty;
    public ObservableCollection<TreeViewNode> Children { get; set; } = new ObservableCollection<TreeViewNode>();

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private Visibility _visibility = Visibility.Visible;
    public Visibility Visibility
    {
        get => _visibility;
        set => SetProperty(ref _visibility, value);
    }
}