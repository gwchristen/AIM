#pragma warning disable MVVMTK0045
using AIM.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool isSearching = false;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isContentSearch = true;

    public ObservableCollection<FileItem> SearchResults { get; } = new();

    public MainViewModel MainViewModel => _mainViewModel;

    public SearchViewModel()
    {
        _mainViewModel = MainWindow.Instance?.ViewModel ?? throw new InvalidOperationException("MainViewModel not available");
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
            // Search in SelectedRoot or all directories
            var rootPath = _mainViewModel.SelectedRoot;
            if (string.IsNullOrEmpty(rootPath))
            {
                StatusMessage = "No root directory selected.";
                return;
            }

            var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
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
            StatusMessage = $"Found {SearchResults.Count} results.";
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