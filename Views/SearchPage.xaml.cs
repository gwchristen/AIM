using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace AIM.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; set; }

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = new SearchViewModel();
    }

    private void SearchButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.SearchResults.Clear();
        var query = ViewModel.SearchQuery;
        if (string.IsNullOrWhiteSpace(query)) return;

        var files = System.IO.Directory.GetFiles(ViewModel.MainViewModel.DirectoryItems[0].FullPath, "*.*", System.IO.SearchOption.AllDirectories)
            .Where(f => System.IO.Path.GetExtension(f).ToLower() is ".txt" or ".csv" or ".log")
            .Where(f => System.IO.File.ReadAllText(f).Contains(query, System.StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileItem
            {
                Name = System.IO.Path.GetFileName(f),
                FullPath = f,
                Type = GetFileType(f),
                Size = new System.IO.FileInfo(f).Length,
                SizeString = $"{new System.IO.FileInfo(f).Length / 1024.0:F2} KB",
                CreatedDate = System.IO.File.GetCreationTime(f),
                ModifiedDate = System.IO.File.GetLastWriteTime(f),
                CreatedDateString = System.IO.File.GetCreationTime(f).ToString("d"),
                ModifiedDateString = System.IO.File.GetLastWriteTime(f).ToString("d"),
                Owner = "N/A"
            });

        foreach (var file in files)
        {
            ViewModel.SearchResults.Add(file);
        }
    }

    private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Navigation removed for now
    }

    private FileType GetFileType(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".txt" => FileType.Text,
            ".csv" => FileType.Csv,
            ".log" => FileType.Log,
            _ => FileType.Other
        };
    }
}