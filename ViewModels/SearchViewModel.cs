using AIM.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private string searchDirectory = string.Empty;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool isSearching = false;

    [ObservableProperty]
    private string statusMessage = "Ready to search.";

    [ObservableProperty]
    private bool isContentSearch = true;

    public ObservableCollection<FileItem> SearchResults { get; } = new();

    public MainViewModel MainViewModel => _mainViewModel;

    public SearchViewModel()
    {
        _mainViewModel = MainWindow.Instance?.ViewModel ?? throw new InvalidOperationException("MainViewModel not available");
        SearchDirectory = _mainViewModel.SelectedRoot;
    }

    [RelayCommand]
    private async Task Browse()
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            SearchDirectory = folder.Path;
        }
    }

    [RelayCommand]
    private async void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        StatusMessage = "Searching...";
        SearchResults.Clear();

        try
        {
            var rootPath = SearchDirectory;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = _mainViewModel.SelectedRoot;
            }
            if (string.IsNullOrEmpty(rootPath))
            {
                StatusMessage = "No search directory selected. Please set the Root Directory in Settings.";
                return;
            }

            var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories).ToList();
            var totalFiles = allFiles.Count;

            var files = allFiles
                .Where(f => Path.GetExtension(f).ToLower() is ".txt" or ".csv" or ".log")
                .Where(f =>
                {
                    if (IsContentSearch)
                    {
                        try
                        {
                            return Path.GetFileName(f).Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   File.ReadAllText(f).Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return Path.GetFileName(f).Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    }
                })
                .Take(100); // Limit results

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var owner = "N/A";
                try
                {
                    var acl = info.GetAccessControl();
                    owner = acl.GetOwner(typeof(System.Security.Principal.NTAccount)).Value;
                }
                catch { }
                var sizeKb = info.Length / 1024.0;
                SearchResults.Add(new FileItem
                {
                    Name = Path.GetFileName(file),
                    FullPath = file,
                    Type = GetFileType(file),
                    Size = info.Length,
                    SizeString = $"{sizeKb:F2} KB",
                    CreatedDate = info.CreationTime,
                    ModifiedDate = info.LastWriteTime,
                    CreatedDateString = info.CreationTime.ToString("d"),
                    ModifiedDateString = info.LastWriteTime.ToString("d"),
                    Owner = owner
                });
            }
            StatusMessage = $"Searched {totalFiles} files. Files found: {SearchResults.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private FileType GetFileType(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".txt" => FileType.Text,
            ".csv" => FileType.Csv,
            ".log" => FileType.Log,
            _ => FileType.Other
        };
    }
}