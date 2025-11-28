using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace AIM.ViewModels;

public partial class InventoryViewerViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IInfoBarService _infoBarService;
    private readonly DispatcherQueue _dispatcherQueue;

    #region Observable Properties
    [ObservableProperty]
    private string _archivePath;

    [ObservableProperty]
    private string _archiveName;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingText = "Loading archive...";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _showTree;

    [ObservableProperty]
    private string _searchQuery;

    [ObservableProperty]
    private bool _hasFilter;

    [ObservableProperty]
    private int _filterMatchCount;

    [ObservableProperty]
    private int _totalFolders;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private string _totalSizeText;
    #endregion

    public ObservableCollection<ArchiveTreeNode> RootNodes { get; } = new();

    public InventoryViewerViewModel(INavigationService navigationService, IInfoBarService infoBarService)
    {
        _navigationService = navigationService;
        _infoBarService = infoBarService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    partial void OnSearchQueryChanged(string value)
    {
        HasFilter = !string.IsNullOrWhiteSpace(value);
        FilterTree(value);
    }

    [RelayCommand]
    private async Task LoadArchiveAsync(string path)
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ShowTree = false;
        RootNodes.Clear();
        ArchivePath = path;
        ArchiveName = Path.GetFileName(path);
        LoadingText = $"Loading {ArchiveName}...";

        try
        {
            var rootDirectoryInfo = new DirectoryInfo(path);
            if (!rootDirectoryInfo.Exists)
            {
                HasError = true;
                ErrorMessage = "The archive folder does not exist or has been moved.";
                return;
            }

            int folderCount = 0;
            int fileCount = 0;
            long totalSize = 0;

            var rootNode = await Task.Run(() =>
            {
                var node = new ArchiveTreeNode
                {
                    Name = rootDirectoryInfo.Name,
                    FullPath = rootDirectoryInfo.FullName,
                    IsFolder = true,
                    IsExpanded = true
                };

                LoadSubdirectoriesAndFiles(rootDirectoryInfo, node, ref folderCount, ref fileCount, ref totalSize);
                return node;
            });

            RootNodes.Add(rootNode);

            TotalFolders = folderCount;
            TotalFiles = fileCount;
            TotalSizeText = FormatFileSize(totalSize);

            IsEmpty = folderCount == 0 && fileCount == 0;
            ShowTree = !IsEmpty;

            if (HasFilter)
            {
                FilterTree(SearchQuery);
            }
        }
        catch (UnauthorizedAccessException)
        {
            HasError = true;
            ErrorMessage = "Access denied. You don't have permission to view this archive.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load archive: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadSubdirectoriesAndFiles(DirectoryInfo directoryInfo, ArchiveTreeNode parentNode, ref int folderCount, ref int fileCount, ref long totalSize)
    {
        try
        {
            foreach (var dir in directoryInfo.GetDirectories())
            {
                folderCount++;
                var dirNode = new ArchiveTreeNode
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    IsFolder = true,
                    IsExpanded = false
                };

                LoadSubdirectoriesAndFiles(dir, dirNode, ref folderCount, ref fileCount, ref totalSize);

                // Set child count for badge
                dirNode.ChildCount = dirNode.Children.Count;
                parentNode.Children.Add(dirNode);
            }

            foreach (var file in directoryInfo.GetFiles())
            {
                fileCount++;
                totalSize += file.Length;

                var fileNode = new ArchiveTreeNode
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsFolder = false,
                    FileSize = file.Length
                };
                parentNode.Children.Add(fileNode);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
    }

    private void FilterTree(string query)
    {
        if (RootNodes.Count == 0) return;

        int matchCount = 0;
        foreach (var node in RootNodes)
        {
            FilterNode(node, query, ref matchCount);
        }

        FilterMatchCount = matchCount;
    }

    private bool FilterNode(ArchiveTreeNode node, string query, ref int matchCount)
    {
        bool isVisible;

        if (string.IsNullOrWhiteSpace(query))
        {
            isVisible = true;
            node.IsExpanded = node.IsFolder && node.Children.Count > 0 && node.Children.Count < 20;
        }
        else
        {
            bool selfMatches = node.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            bool childrenMatch = false;

            foreach (var child in node.Children)
            {
                if (FilterNode(child, query, ref matchCount))
                {
                    childrenMatch = true;
                }
            }

            isVisible = selfMatches || childrenMatch;

            if (selfMatches && !node.IsFolder)
            {
                matchCount++;
            }

            // Expand nodes that have matching children
            if (childrenMatch)
            {
                node.IsExpanded = true;
            }
        }

        node.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        return isVisible;
    }

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var node in RootNodes)
        {
            SetExpandedState(node, true);
        }
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var node in RootNodes)
        {
            SetExpandedState(node, false);
        }
    }

    private void SetExpandedState(ArchiveTreeNode node, bool expanded)
    {
        if (node.IsFolder)
        {
            node.IsExpanded = expanded;
            foreach (var child in node.Children)
            {
                SetExpandedState(child, expanded);
            }
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void PreviewFile(ArchiveTreeNode node)
    {
        if (node == null || node.IsFolder) return;

        var fileItem = new FileItem
        {
            Name = node.Name,
            FullPath = node.FullPath,
            Type = Path.GetExtension(node.FullPath).ToLower() == ".csv" ? FileType.Csv : FileType.Text
        };

        _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
    }

    [RelayCommand]
    private async Task OpenInExplorerAsync()
    {
        if (!string.IsNullOrEmpty(ArchivePath) && Directory.Exists(ArchivePath))
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(ArchivePath);
        }
    }

    public async Task OpenNodeInExplorerAsync(ArchiveTreeNode node)
    {
        if (node == null) return;

        try
        {
            var path = node.IsFolder ? node.FullPath : Path.GetDirectoryName(node.FullPath);
            await Windows.System.Launcher.LaunchFolderPathAsync(path);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void CopyPath(ArchiveTreeNode node)
    {
        if (node == null) return;

        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(node.FullPath);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        _infoBarService.Show("Copied", "Path copied to clipboard.", InfoBarSeverity.Success, 2000);
    }

    [RelayCommand]
    private void Retry()
    {
        if (!string.IsNullOrEmpty(ArchivePath))
        {
            LoadArchiveCommand.Execute(ArchivePath);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        SearchQuery = string.Empty;
        _navigationService.NavigateTo(typeof(InventoryAdminToolsPage));
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
}

public partial class ArchiveTreeNode : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public long FileSize { get; set; }
    public int ChildCount { get; set; }

    public ObservableCollection<ArchiveTreeNode> Children { get; set; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private Visibility _visibility = Visibility.Visible;

    public string Icon => IsFolder ? "\uE8B7" : GetFileIcon();

    public SolidColorBrush IconColor => IsFolder
        ? new SolidColorBrush(Color.FromArgb(255, 255, 183, 77))
        : GetFileIconColor();

    public Visibility HasBadge => (IsFolder && ChildCount > 0) || (!IsFolder && FileSize > 0)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string BadgeText => IsFolder
        ? $"{ChildCount} items"
        : FormatFileSize(FileSize);

    private string GetFileIcon()
    {
        var ext = Path.GetExtension(FullPath).ToLower();
        return ext switch
        {
            ".txt" => "\uE8A5",
            ". csv" => "\uE9D9",
            ". pdf" => "\uEA90",
            ".jpg" or ".jpeg" or ".png" or ". gif" or ".bmp" => "\uEB9F",
            ". doc" or ".docx" => "\uE8A5",
            ". xls" or ". xlsx" => "\uE9D9",
            _ => "\uE8A5"
        };
    }

    private SolidColorBrush GetFileIconColor()
    {
        var ext = Path.GetExtension(FullPath).ToLower();
        return ext switch
        {
            ".txt" => new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)),
            ".csv" => new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
            ".pdf" => new SolidColorBrush(Color.FromArgb(255, 244, 67, 54)),
            ".jpg" or ".jpeg" or ". png" or ".gif" or ".bmp" => new SolidColorBrush(Color.FromArgb(255, 156, 39, 176)),
            ".doc" or ".docx" => new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)),
            ".xls" or ".xlsx" => new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
            _ => new SolidColorBrush(Color.FromArgb(255, 158, 158, 158))
        };
    }

    private static string FormatFileSize(long bytes)
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
}