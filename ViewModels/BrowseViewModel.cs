#pragma warning disable MVVMTK0045
using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class BrowseViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private FileItem selectedFile;

    public event Action<string, string> RenameRequested;
    public event Action<FileItem> DeleteRequested;

    public ObservableCollection<FileItem> Files { get; } = new();

    public BrowseViewModel()
    {
        _fileService = Ioc.Default.GetService<IFileService>();
        _mainViewModel = MainWindow.Instance?.ViewModel ?? throw new InvalidOperationException("MainViewModel not available");
    }

    public async Task LoadFilesAsync(DirectoryItem item)
    {
        Files.Clear();
        try
        {
            var files = Directory.GetFiles(item.FullPath)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLower();
                    return ext == ".txt" || ext == ".csv" || ext == ".log";
                })
                .Select(f => new FileItem { Name = Path.GetFileName(f), FullPath = f, Type = GetFileType(f) });
            foreach (var file in files)
            {
                Files.Add(file);
            }
        }
        catch { }
    }

    public async Task LoadFilesAsync(string path)
    {
        var item = new DirectoryItem { FullPath = path, Name = Path.GetFileName(path) };
        await LoadFilesAsync(item);
    }

    [RelayCommand]
    private void RenameItem()
    {
        if (SelectedFile == null) return;
        RenameRequested?.Invoke(SelectedFile.FullPath, SelectedFile.Name);
    }

    [RelayCommand]
    private void DeleteToArchive()
    {
        if (SelectedFile == null) return;
        DeleteRequested?.Invoke(SelectedFile);
    }

    public void CompleteRename(string newName)
    {
        if (SelectedFile == null) return;
        var newPath = Path.Combine(Path.GetDirectoryName(SelectedFile.FullPath), newName);
        File.Move(SelectedFile.FullPath, newPath);
        SelectedFile.Name = newName;
        SelectedFile.FullPath = newPath;
    }

    public void CompleteDelete()
    {
        if (SelectedFile == null) return;
        var archiveDir = _mainViewModel.ArchivePath;
        Directory.CreateDirectory(archiveDir);
        var archivePath = Path.Combine(archiveDir, SelectedFile.Name);
        File.Move(SelectedFile.FullPath, archivePath);
        Files.Remove(SelectedFile);
        SelectedFile = null;
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