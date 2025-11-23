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

    private string? _savedLeft1Path;
    private string? _savedLeft2Path;
    private string? _savedLeft3Path;
    private string? _savedRight1Path;
    private string? _savedRight2Path;
    private string? _savedRight3Path;

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

    [ObservableProperty]
    private ContentItem selectedRightContent;

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

        // Add handler to refresh levels when directory tree changes (e.g., root update)
        _mainViewModel.DirectoryItems.CollectionChanged += (s, e) =>
        {
            PopulateLevels();
            UpdateRightSelectedDirectory();
        };
    }

    private void PopulateLevels()
    {
        if (DirectoryTree.Count == 0)
        {
            _savedLeft1Path = SelectedLeftLevel1?.FullPath;
            _savedLeft2Path = SelectedLeftLevel2?.FullPath;
            _savedLeft3Path = SelectedLeftLevel3?.FullPath;
            _savedRight1Path = SelectedRightLevel1?.FullPath;
            _savedRight2Path = SelectedRightLevel2?.FullPath;
            _savedRight3Path = SelectedRightLevel3?.FullPath;
            return;
        }

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

        // Restore left selections
        SelectedLeftLevel1 = LeftLevel1.FirstOrDefault(d => d.FullPath == _savedLeft1Path);
        if (SelectedLeftLevel1 != null)
        {
            LeftLevel2.Clear();
            foreach (var sub in SelectedLeftLevel1.SubDirectories.Where(s => HasContents(s)))
            {
                LeftLevel2.Add(sub);
            }
            SelectedLeftLevel2 = LeftLevel2.FirstOrDefault(d => d.FullPath == _savedLeft2Path);
            if (SelectedLeftLevel2 != null)
            {
                LeftLevel3.Clear();
                foreach (var sub in SelectedLeftLevel2.SubDirectories.Where(s => HasContents(s)))
                {
                    LeftLevel3.Add(sub);
                }
                SelectedLeftLevel3 = LeftLevel3.FirstOrDefault(d => d.FullPath == _savedLeft3Path);
            }
        }

        // Restore right selections
        SelectedRightLevel1 = RightLevel1.FirstOrDefault(d => d.FullPath == _savedRight1Path);
        if (SelectedRightLevel1 != null)
        {
            RightLevel2.Clear();
            foreach (var sub in SelectedRightLevel1.SubDirectories)
            {
                RightLevel2.Add(sub);
            }
            SelectedRightLevel2 = RightLevel2.FirstOrDefault(d => d.FullPath == _savedRight2Path);
            if (SelectedRightLevel2 != null)
            {
                RightLevel3.Clear();
                foreach (var sub in SelectedRightLevel2.SubDirectories)
                {
                    RightLevel3.Add(sub);
                }
                SelectedRightLevel3 = RightLevel3.FirstOrDefault(d => d.FullPath == _savedRight3Path);
            }
        }

        // Clear saved paths
        _savedLeft1Path = null;
        _savedLeft2Path = null;
        _savedLeft3Path = null;
        _savedRight1Path = null;
        _savedRight2Path = null;
        _savedRight3Path = null;

        UpdateLeftSelectedDirectory();
        UpdateRightSelectedDirectory();
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
        if (item == null) return;
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
        var originalPath = SelectedFile.FullPath;
        var archivePath = Path.Combine(_mainViewModel.ArchivePath, SelectedFile.Name);
        File.Move(SelectedFile.FullPath, archivePath);
        _lastAction = new UndoAction { Type = "Archive", FromPath = originalPath, ToPath = archivePath };
        Files.Remove(SelectedFile);
        SelectedFile = null;

        // Refresh left content list to remove the deleted item
        UpdateFilteredContents();
    }

    [RelayCommand]
    private void ShipItems()
    {
        if (SelectedFile == null) return;
        var originalPath = SelectedFile.FullPath;
        var shippedPath = Path.Combine(_mainViewModel.ShippedDirectory, SelectedFile.Name);
        File.Move(SelectedFile.FullPath, shippedPath);
        _lastAction = new UndoAction { Type = "Ship", FromPath = originalPath, ToPath = shippedPath };
        ShipRequested?.Invoke(SelectedFile);
        Files.Remove(SelectedFile);
        SelectedFile = null;

        // Refresh left content list to remove shipped items
        UpdateFilteredContents();
    }

    [RelayCommand]
    private void MoveFile()
    {
        if (SelectedFile == null || SelectedRightDirectory == null) return;
        var newPath = Path.Combine(SelectedRightDirectory.FullPath, SelectedFile.Name);
        File.Move(SelectedFile.FullPath, newPath);
        _lastAction = new UndoAction { Type = "Move", FromPath = SelectedFile.FullPath, ToPath = newPath };
        Files.Remove(SelectedFile);
        SelectedFile = null;

        // Refresh right content list to show the moved file
        UpdateRightFilteredContents();
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
        var oldPath = SelectedFile.FullPath;
        var newPath = Path.Combine(Path.GetDirectoryName(SelectedFile.FullPath), newName);
        File.Move(SelectedFile.FullPath, newPath);
        _lastAction = new UndoAction { Type = "Rename", FromPath = oldPath, ToPath = newPath, NewName = SelectedFile.Name };
        SelectedFile.Name = newName;
        SelectedFile.FullPath = newPath;

        // Refresh left content list to reflect the rename
        UpdateFilteredContents();
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

        // Refresh left content list to remove the shipped item
        UpdateFilteredContents();
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

    [RelayCommand]
    private void CopyFromScans()
    {
        if (SelectedRightDirectory == null || _mainViewModel.SelectedScanFiles.Count == 0) return;

        foreach (var file in _mainViewModel.SelectedScanFiles)
        {
            var dest = Path.Combine(SelectedRightDirectory.FullPath, file.Name);
            try
            {
                File.Copy(file.FullPath, dest, true); // Overwrite if exists
            }
            catch (Exception ex)
            {
                // Optionally log or show error
            }
        }

        // Refresh the file list
        _ = LoadFilesAsync(SelectedRightDirectory);
    }

    public void NavigateToRightDirectory(DirectoryItem item)
    {
        SelectedRightDirectory = item;
        UpdateRightFilteredContents();

        // Update combo boxes to reflect the path
        var root = DirectoryTree.Count > 0 ? DirectoryTree[0] : null;
        if (root != null && item.FullPath.StartsWith(root.FullPath))
        {
            var relative = item.FullPath.Substring(root.FullPath.Length).TrimStart(Path.DirectorySeparatorChar);
            var parts = relative.Split(Path.DirectorySeparatorChar);
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
            {
                SelectedRightLevel1 = RightLevel1.FirstOrDefault(d => d.Name == parts[0]);
                if (SelectedRightLevel1 != null && parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    // Populate and set Level2
                    RightLevel2.Clear();
                    foreach (var sub in SelectedRightLevel1.SubDirectories)
                    {
                        RightLevel2.Add(sub);
                    }
                    SelectedRightLevel2 = RightLevel2.FirstOrDefault(d => d.Name == parts[1]);
                    if (SelectedRightLevel2 != null && parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                    {
                        // Populate and set Level3
                        RightLevel3.Clear();
                        foreach (var sub in SelectedRightLevel2.SubDirectories)
                        {
                            RightLevel3.Add(sub);
                        }
                        SelectedRightLevel3 = RightLevel3.FirstOrDefault(d => d.Name == parts[2]);
                    }
                    else
                    {
                        SelectedRightLevel3 = null;
                        RightLevel3.Clear();
                    }
                }
                else
                {
                    SelectedRightLevel2 = null;
                    SelectedRightLevel3 = null;
                    RightLevel2.Clear();
                    RightLevel3.Clear();
                }
            }
            else
            {
                SelectedRightLevel1 = null;
                SelectedRightLevel2 = null;
                SelectedRightLevel3 = null;
                RightLevel2.Clear();
                RightLevel3.Clear();
            }
        }
    }

    private class UndoAction
    {
        public string Type { get; set; } = string.Empty;
        public string FromPath { get; set; } = string.Empty;
        public string ToPath { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
    }

    private UndoAction? _lastAction;

    [RelayCommand]
    private async Task Undo()
    {
        if (_lastAction == null) return;

        try
        {
            switch (_lastAction.Type)
            {
                case "Move":
                    File.Move(_lastAction.ToPath, _lastAction.FromPath);
                    break;
                case "Rename":
                    // Rename back: from new path to original
                    var originalPath = Path.Combine(Path.GetDirectoryName(_lastAction.ToPath)!, _lastAction.NewName);
                    File.Move(_lastAction.ToPath, originalPath);
                    break;
                case "Archive":
                    // Move back from archive
                    var archivePath = Path.Combine(_mainViewModel.ArchivePath, Path.GetFileName(_lastAction.FromPath));
                    File.Move(archivePath, _lastAction.FromPath);
                    break;
                case "Ship":
                    // Move back from shipped
                    var shippedPath = Path.Combine(_mainViewModel.ShippedDirectory, Path.GetFileName(_lastAction.FromPath));
                    File.Move(shippedPath, _lastAction.FromPath);
                    break;
            }
            _lastAction = null;

            // Refresh UI immediately
            await LoadFilesAsync(SelectedLeftDirectory);
            UpdateFilteredContents();
            UpdateRightFilteredContents();
        }
        catch (Exception ex)
        {
            // Optionally log or show error
        }
    }
}