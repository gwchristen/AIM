using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class BrowseViewModel : ObservableObject
{
    #region Services and Private Fields
    private readonly MainViewModel _mainViewModel;
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly AppSettings _appSettings;
    private UndoAction? _lastAction;
    #endregion

    #region Observable Collections
    public ObservableCollection<DirectoryItem> LeftLevel1 { get; } = new();
    public ObservableCollection<DirectoryItem> LeftLevel2 { get; } = new();
    public ObservableCollection<DirectoryItem> LeftLevel3 { get; } = new();
    public ObservableCollection<DirectoryItem> RightLevel1 { get; } = new();
    public ObservableCollection<DirectoryItem> RightLevel2 { get; } = new();
    public ObservableCollection<DirectoryItem> RightLevel3 { get; } = new();
    public ObservableCollection<ContentItem> RightFilteredContents { get; } = new();
    public ObservableCollection<FileItem> Files { get; } = new();
    #endregion

    #region Observable Properties
    [ObservableProperty] private DirectoryItem _selectedLeftLevel1;
    [ObservableProperty] private DirectoryItem _selectedLeftLevel2;
    [ObservableProperty] private DirectoryItem _selectedLeftLevel3;
    [ObservableProperty] private DirectoryItem _selectedRightLevel1;
    [ObservableProperty] private DirectoryItem _selectedRightLevel2;
    [ObservableProperty] private DirectoryItem _selectedRightLevel3;
    [ObservableProperty] private ContentItem _selectedRightContent;
    [ObservableProperty] private FileItem _selectedFile;
    [ObservableProperty] private DirectoryItem _selectedLeftDirectory;
    [ObservableProperty] private DirectoryItem _selectedRightDirectory;
    [ObservableProperty] private string _rootName = string.Empty;
    #endregion

    public BrowseViewModel(
        MainViewModel mainViewModel,
        IFileService fileService,
        ISettingsService settingsService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _mainViewModel = mainViewModel;
        _fileService = fileService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _appSettings = settingsService.LoadSettings();

        _mainViewModel.LeftTree.CollectionChanged += (s, e) => PopulateAllLevels();
        PopulateAllLevels();
    }

    #region Directory and File Loading Logic
    private void PopulateAllLevels()
    {
        if (_mainViewModel.LeftTree.Count == 0) return;

        var root = _mainViewModel.LeftTree[0];
        RootName = root.Name;

        LeftLevel1.Clear();
        RightLevel1.Clear();
        foreach (var sub in root.SubDirectories)
        {
            RightLevel1.Add(sub);
            if (HasContents(sub))
            {
                LeftLevel1.Add(sub);
            }
        }
        ClearLeftSelections(1);
        ClearRightSelections(1);
    }

    private bool HasContents(DirectoryItem item)
    {
        try
        {
            return item.SubDirectories.Any() || Directory.EnumerateFileSystemEntries(item.FullPath).Any();
        }
        catch { return false; }
    }

    partial void OnSelectedLeftLevel1Changed(DirectoryItem value)
    {
        LeftLevel2.Clear();
        if (value != null)
        {
            foreach (var sub in value.SubDirectories.Where(HasContents))
            {
                LeftLevel2.Add(sub);
            }
        }
        ClearLeftSelections(2);
    }

    partial void OnSelectedLeftLevel2Changed(DirectoryItem value)
    {
        LeftLevel3.Clear();
        if (value != null)
        {
            foreach (var sub in value.SubDirectories.Where(HasContents))
            {
                LeftLevel3.Add(sub);
            }
        }
        ClearLeftSelections(3);
    }

    partial void OnSelectedLeftLevel3Changed(DirectoryItem value) => UpdateLeftDirectory();

    partial void OnSelectedRightLevel1Changed(DirectoryItem value)
    {
        RightLevel2.Clear();
        if (value != null)
        {
            foreach (var sub in value.SubDirectories)
            {
                RightLevel2.Add(sub);
            }
        }
        ClearRightSelections(2);
    }

    partial void OnSelectedRightLevel2Changed(DirectoryItem value)
    {
        RightLevel3.Clear();
        if (value != null)
        {
            foreach (var sub in value.SubDirectories)
            {
                RightLevel3.Add(sub);
            }
        }
        ClearRightSelections(3);
    }

    partial void OnSelectedRightLevel3Changed(DirectoryItem value) => UpdateRightDirectory();

    [RelayCommand]
    private void ClearLeftSelections(int fromLevel)
    {
        if (fromLevel <= 1) SelectedLeftLevel1 = null;
        if (fromLevel <= 2) SelectedLeftLevel2 = null;
        if (fromLevel <= 3) SelectedLeftLevel3 = null;
        UpdateLeftDirectory();
    }

    [RelayCommand]
    private void ClearRightSelections(int fromLevel)
    {
        if (fromLevel <= 1) SelectedRightLevel1 = null;
        if (fromLevel <= 2) SelectedRightLevel2 = null;
        if (fromLevel <= 3) SelectedRightLevel3 = null;
        UpdateRightDirectory();
    }

    private void UpdateLeftDirectory()
    {
        SelectedLeftDirectory = SelectedLeftLevel3 ?? SelectedLeftLevel2 ?? SelectedLeftLevel1;
        LoadFiles(SelectedLeftDirectory);
    }

    private void UpdateRightDirectory()
    {
        SelectedRightDirectory = SelectedRightLevel3 ?? SelectedRightLevel2 ?? SelectedRightLevel1;
        UpdateRightFilteredContents();
    }

    private void UpdateRightFilteredContents()
    {
        RightFilteredContents.Clear();
        var dir = SelectedRightDirectory ?? (_mainViewModel.LeftTree.Count > 0 ? _mainViewModel.LeftTree[0] : null);
        if (dir == null) return;

        foreach (var sub in dir.SubDirectories)
        {
            RightFilteredContents.Add(new ContentItem { Name = sub.Name, IsFolder = true, FullPath = sub.FullPath });
        }
        try
        {
            foreach (var file in Directory.GetFiles(dir.FullPath))
            {
                RightFilteredContents.Add(new ContentItem { Name = Path.GetFileName(file), IsFolder = false, FullPath = file });
            }
        }
        catch { }
    }

    private void LoadFiles(DirectoryItem item)
    {
        Files.Clear();
        if (item == null) return;

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
    #endregion

    #region File Operations Commands
    [RelayCommand]
    private async Task RenameFile()
    {
        if (SelectedFile == null) return;
        var newName = await _dialogService.ShowRenameDialogAsync(SelectedFile.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == SelectedFile.Name) return;

        var oldPath = SelectedFile.FullPath;
        var newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName);

        try
        {
            File.Move(oldPath, newPath);
            _lastAction = new UndoAction { Type = "Rename", FromPath = oldPath, ToPath = newPath };
            SelectedFile.Name = newName;
            SelectedFile.FullPath = newPath;
        }
        catch (Exception ex) { /* Handle error */ }
    }

    [RelayCommand]
    private async Task ArchiveFile()
    {
        if (SelectedFile == null) return;
        var confirmed = await _dialogService.ShowConfirmationDialogAsync("Archive File", $"Move '{SelectedFile.Name}' to archive?");
        if (!confirmed) return;

        var archivePath = _appSettings.ArchivePath;
        if (string.IsNullOrEmpty(archivePath)) return;
        Directory.CreateDirectory(archivePath);
        var destPath = Path.Combine(archivePath, SelectedFile.Name);

        try
        {
            File.Move(SelectedFile.FullPath, destPath);
            _lastAction = new UndoAction { Type = "Archive", FromPath = SelectedFile.FullPath, ToPath = destPath };
            Files.Remove(SelectedFile);
        }
        catch (Exception ex) { /* Handle error */ }
    }

    [RelayCommand]
    private async Task ShipFile()
    {
        if (SelectedFile == null) return;
        var confirmed = await _dialogService.ShowConfirmationDialogAsync("Ship File", $"Move '{SelectedFile.Name}' to shipped folder?");
        if (!confirmed) return;

        var shippedPath = _appSettings.ShippedDirectory;
        if (string.IsNullOrEmpty(shippedPath)) return;
        Directory.CreateDirectory(shippedPath);
        var destPath = Path.Combine(shippedPath, SelectedFile.Name);

        try
        {
            File.Move(SelectedFile.FullPath, destPath);
            _lastAction = new UndoAction { Type = "Ship", FromPath = SelectedFile.FullPath, ToPath = destPath };
            Files.Remove(SelectedFile);
        }
        catch (Exception ex) { /* Handle error */ }
    }

    [RelayCommand]
    private void MoveFile()
    {
        if (SelectedFile == null || SelectedRightDirectory == null) return;
        var destPath = Path.Combine(SelectedRightDirectory.FullPath, SelectedFile.Name);

        try
        {
            File.Move(SelectedFile.FullPath, destPath);
            _lastAction = new UndoAction { Type = "Move", FromPath = SelectedFile.FullPath, ToPath = destPath };
            Files.Remove(SelectedFile);
            UpdateRightFilteredContents();
        }
        catch (Exception ex) { /* Handle error */ }
    }

    [RelayCommand]
    private void CopyFromScans()
    {
        if (SelectedRightDirectory == null || _mainViewModel.SelectedScanFiles.Count == 0) return;

        foreach (var file in _mainViewModel.SelectedScanFiles)
        {
            try
            {
                var dest = Path.Combine(SelectedRightDirectory.FullPath, file.Name);
                File.Copy(file.FullPath, dest, true);
            }
            catch (Exception ex) { /* Handle error */ }
        }
        UpdateRightFilteredContents();
    }

    [RelayCommand]
    private void Undo()
    {
        if (_lastAction == null) return;

        try
        {
            File.Move(_lastAction.ToPath, _lastAction.FromPath);
            _lastAction = null;
            // Refresh views
            LoadFiles(SelectedLeftDirectory);
            UpdateRightFilteredContents();
        }
        catch (Exception ex) { /* Handle error */ }
    }

    [RelayCommand]
    private void NavigateToPreview()
    {
        if (SelectedFile != null)
        {
            _navigationService.NavigateTo(typeof(PreviewPage), SelectedFile);
        }
    }

    partial void OnSelectedRightContentChanged(ContentItem value)
    {
        if (value?.IsFolder == true)
        {
            // When a folder is clicked in the right-hand list, navigate into it.
            // This is complex logic that would need to find the full DirectoryItem
            // and update the RightLevel combo boxes. For now, we'll just update the content.
            var dir = new DirectoryItem { FullPath = value.FullPath, Name = value.Name };
            _fileService.PopulateSubDirectories(dir); // Make sure it has children
            SelectedRightDirectory = dir;
            UpdateRightFilteredContents();
        }
    }
    #endregion

    private class UndoAction
    {
        public string Type { get; set; }
        public string FromPath { get; set; }
        public string ToPath { get; set; }
    }
}