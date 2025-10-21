using AIM.Models;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class ScansViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private DirectoryItem selectedDirectory;

    [ObservableProperty]
    private ObservableCollection<FileItem> selectedFiles = new();

    public ObservableCollection<DirectoryItem> Directories { get; } = new();
    public ObservableCollection<FileItem> Files { get; } = new();

    public ScansViewModel()
    {
        _mainViewModel = MainWindow.Instance?.ViewModel ?? throw new InvalidOperationException("MainViewModel not available");
        PopulateDirectories();
    }

    private void PopulateDirectories()
    {
        Directories.Clear();
        if (!Directory.Exists(_mainViewModel.FileScansDirectory)) return;

        var root = new DirectoryItem { Name = Path.GetFileName(_mainViewModel.FileScansDirectory) ?? "Scans", FullPath = _mainViewModel.FileScansDirectory };
        Directories.Add(root);
        // Optionally add subdirectories flat
        try
        {
            var subs = Directory.GetDirectories(root.FullPath).Select(d => new DirectoryItem { Name = Path.GetFileName(d), FullPath = d });
            foreach (var sub in subs)
            {
                Directories.Add(sub);
            }
        }
        catch { }
    }

    partial void OnSelectedDirectoryChanged(DirectoryItem value)
    {
        if (value != null)
        {
            _ = LoadFiles(value);
        }
    }

    partial void OnSelectedFilesChanged(ObservableCollection<FileItem> value)
    {
        _mainViewModel.SelectedScanFiles = new ObservableCollection<FileItem>(value);
    }

    private async Task LoadFiles(DirectoryItem item)
    {
        Files.Clear();
        try
        {
            var files = Directory.GetFiles(item.FullPath).Select(f =>
            {
                var info = new FileInfo(f);
                var type = GetFileType(f);
                var sizeKb = info.Length / 1024.0;
                return new FileItem
                {
                    Name = Path.GetFileName(f),
                    FullPath = f,
                    Type = type,
                    Size = info.Length,
                    SizeString = $"{sizeKb:F2} KB",
                    CreatedDate = info.CreationTime,
                    ModifiedDate = info.LastWriteTime,
                    CreatedDateString = info.CreationTime.ToString("d"),
                    ModifiedDateString = info.LastWriteTime.ToString("d"),
                };
            });
            foreach (var file in files)
            {
                Files.Add(file);
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task OpenFile(FileItem file)
    {
        // Navigate to Preview tab and load file
        if (MainWindow.Instance != null)
        {
            MainWindow.Instance.MainFrame.Navigate(typeof(PreviewPage));
            // Set the selected tab
            MainWindow.Instance.IsPreviewSelected = true;
            MainWindow.Instance.IsBrowseSelected = false;
            MainWindow.Instance.IsSearchSelected = false;
            MainWindow.Instance.IsScansSelected = false;
            MainWindow.Instance.IsInvArchivesSelected = false;
            MainWindow.Instance.IsStatsSelected = false;
            MainWindow.Instance.IsSettingsSelected = false;

            // Load the file in Preview
            if (MainWindow.Instance.MainFrame.Content is PreviewPage previewPage)
            {
                var fileItem = new FileItem { FullPath = file.FullPath, Name = file.Name, Type = file.Type };
                await previewPage.ViewModel.LoadFileContent(fileItem);
            }
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