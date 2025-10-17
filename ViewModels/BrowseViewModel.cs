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
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class BrowseViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private FileItem selectedFile;

    [ObservableProperty]
    private DirectoryItem selectedDirectory;

    [ObservableProperty]
    private DirectoryItem selectedLevel0;

    [ObservableProperty]
    private DirectoryItem selectedLevel1;

    [ObservableProperty]
    private DirectoryItem selectedLevel2;

    [ObservableProperty]
    private DirectoryItem selectedLevel3;

    [ObservableProperty]
    private ContentItem selectedContent;

    [ObservableProperty]
    private DirectoryItem selectedLeftDirectory;

    [ObservableProperty]
    private DirectoryItem selectedRightDirectory;

    [ObservableProperty]
    private DirectoryItem selectedLeftLevel1;

    [ObservableProperty]
    private DirectoryItem selectedLeftLevel2;

    [ObservableProperty]
    private DirectoryItem selectedLeftLevel3;

    [ObservableProperty]
    private DirectoryItem selectedRightLevel1;

    [ObservableProperty]
    private DirectoryItem selectedRightLevel2;

    [ObservableProperty]
    private DirectoryItem selectedRightLevel3;

    [ObservableProperty]
    private string rootName = string.Empty;

    public event Action<string, string> RenameRequested;
    public event Action<FileItem> DeleteRequested;
    public event Action<FileItem> ShipRequested;

    public ObservableCollection<FileItem> Files { get; } = new();
    public ObservableCollection<DirectoryItem> DirectoryTree => _mainViewModel.DirectoryItems;

    public ObservableCollection<DirectoryItem> Level1 { get; } = new();
    public ObservableCollection<DirectoryItem> Level2 { get; } = new();
    public ObservableCollection<DirectoryItem> Level3 { get; } = new();

    public ObservableCollection<DirectoryItem> LeftLevel1 { get; } = new();
    public ObservableCollection<DirectoryItem> LeftLevel2 { get; } = new();
    public ObservableCollection<DirectoryItem> LeftLevel3 { get; } = new();

    public ObservableCollection<DirectoryItem> RightLevel1 { get; } = new();
    public ObservableCollection<DirectoryItem> RightLevel2 { get; } = new();
    public ObservableCollection<DirectoryItem> RightLevel3 { get; } = new();

    public ObservableCollection<ContentItem> FilteredContents { get; } = new();
    public ObservableCollection<ContentItem> RightFilteredContents { get; } = new();

    public BrowseViewModel()
    {
        _fileService = Ioc.Default.GetService<IFileService>();
        _mainViewModel = MainWindow.Instance?.ViewModel ?? throw new InvalidOperationException("MainViewModel not available");
        PopulateLevels();
        UpdateFilteredContents();
        UpdateRightFilteredContents();
    }

    private void PopulateLevels()
    {
        Level1.Clear();
        Level2.Clear();
        Level3.Clear();
        LeftLevel1.Clear();
        LeftLevel2.Clear();
        LeftLevel3.Clear();
        RightLevel1.Clear();
        RightLevel2.Clear();
        RightLevel3.Clear();
        if (DirectoryTree.Count > 0)
        {
            var root = DirectoryTree[0];
            RootName = root.Name;
            foreach (var sub in root.SubDirectories)
            {
                Level1.Add(sub);
                if (HasContents(sub))
                {
                    LeftLevel1.Add(sub);
                }
                RightLevel1.Add(sub);
            }
        }
    }

    public bool HasContents(DirectoryItem item)
    {
        try
        {
            return item.SubDirectories.Any() || Directory.GetFiles(item.FullPath).Any();
        }
        catch
        {
            return false;
        }
    }

    public void UpdateFilteredContents()
    {
        FilteredContents.Clear();
        var currentDirectory = SelectedDirectory ?? (DirectoryTree.Count > 0 ? DirectoryTree[0] : null);
        if (currentDirectory != null)
        {
            foreach (var sub in currentDirectory.SubDirectories)
            {
                FilteredContents.Add(new ContentItem { Name = sub.Name, IsFolder = true, FullPath = sub.FullPath });
            }
            try
            {
                var files = Directory.GetFiles(currentDirectory.FullPath)
                    .Select(f => new ContentItem { Name = Path.GetFileName(f), IsFolder = false, FullPath = f });
                foreach (var file in files)
                {
                    FilteredContents.Add(file);
                }
            }
            catch { }
        }
    }

    public void UpdateRightFilteredContents()
    {
        RightFilteredContents.Clear();
        var currentDirectory = SelectedRightDirectory ?? (DirectoryTree.Count > 0 ? DirectoryTree[0] : null);
        if (currentDirectory != null)
        {
            foreach (var sub in currentDirectory.SubDirectories)
            {
                RightFilteredContents.Add(new ContentItem { Name = sub.Name, IsFolder = true, FullPath = sub.FullPath });
            }
            try
            {
                var files = Directory.GetFiles(currentDirectory.FullPath)
                    .Select(f => new ContentItem { Name = Path.GetFileName(f), IsFolder = false, FullPath = f });
                foreach (var file in files)
                {
                    RightFilteredContents.Add(file);
                }
            }
            catch { }
        }
    }

    public async Task LoadFilesAsync(DirectoryItem item)
    {
        Files.Clear();
        try
        {
            var files = Directory.GetFiles(item.FullPath)
                .Select(f =>
                {
                    var info = new FileInfo(f);
                    var owner = "N/A";
                    try
                    {
                        var acl = info.GetAccessControl();
                        owner = acl.GetOwner(typeof(NTAccount)).Value;
                    }
                    catch { }
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
                        Owner = owner
                    };
                });
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

    [RelayCommand]
    private void ShipItems()
    {
        if (SelectedFile == null) return;
        ShipRequested?.Invoke(SelectedFile);
    }

    [RelayCommand]
    private void MoveFile()
    {
        if (SelectedFile == null || SelectedRightDirectory == null) return;
        var newPath = Path.Combine(SelectedRightDirectory.FullPath, SelectedFile.Name);
        File.Move(SelectedFile.FullPath, newPath);
        Files.Remove(SelectedFile);
        SelectedFile = null;
    }

    public void UpdateSelectedDirectory()
    {
        SelectedDirectory = SelectedLevel3 ?? SelectedLevel2 ?? SelectedLevel1 ?? (DirectoryTree.Count > 0 ? DirectoryTree[0] : null);
        UpdateFilteredContents();
    }

    public void UpdateLeftSelectedDirectory()
    {
        SelectedLeftDirectory = SelectedLeftLevel3 ?? SelectedLeftLevel2 ?? SelectedLeftLevel1 ?? (DirectoryTree.Count > 0 ? DirectoryTree[0] : null);
        _ = LoadFilesAsync(SelectedLeftDirectory);
    }

    public void UpdateLeftSelectedDirectory(DirectoryItem item)
    {
        SelectedLeftDirectory = item;
        _ = LoadFilesAsync(SelectedLeftDirectory);
    }

    public void UpdateRightSelectedDirectory()
    {
        SelectedRightDirectory = SelectedRightLevel3 ?? SelectedRightLevel2 ?? SelectedRightLevel1 ?? (DirectoryTree.Count > 0 ? DirectoryTree[0] : null);
        UpdateRightFilteredContents();
    }

    public void UpdateRightSelectedDirectory(DirectoryItem item)
    {
        SelectedRightDirectory = item;
        UpdateRightFilteredContents();
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

    public void CompleteShip()
    {
        if (SelectedFile == null) return;
        var shippedDir = _mainViewModel.ShippedDirectory;
        if (string.IsNullOrEmpty(shippedDir))
        {
            return;
        }
        Directory.CreateDirectory(shippedDir);
        var shippedPath = Path.Combine(shippedDir, SelectedFile.Name);
        File.Move(SelectedFile.FullPath, shippedPath);
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