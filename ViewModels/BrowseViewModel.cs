using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public record FileOp(string FromPath, string ToPath);
public record UndoAction(string Type, List<FileOp> Ops);

public partial class BrowseViewModel : ObservableObject
{
    #region Services and Private Fields
    private readonly MainViewModel _mainViewModel;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly AppSettings _appSettings;
    private Stack<UndoAction> _undoStack = new();
    private string _rootPath = string.Empty;
    private string _currentSortColumn = "Name";
    private bool _isSortAscending = true;
    #endregion

    #region Observable Collections
    public ObservableCollection<BreadcrumbItem> LeftBreadcrumbs { get; } = new();
    public ObservableCollection<BreadcrumbItem> RightBreadcrumbs { get; } = new();
    public ObservableCollection<ContentItem> LeftFilteredContents { get; } = new();
    public ObservableCollection<ContentItem> RightFilteredContents { get; } = new();
    [ObservableProperty] private ObservableCollection<object> _selectedLeftItems = new();
    #endregion

    #region Observable Properties
    [ObservableProperty] private ContentItem _selectedRightContent;
    [ObservableProperty] private DirectoryItem _selectedLeftDirectory;
    [ObservableProperty] private DirectoryItem _selectedRightDirectory;
    [ObservableProperty] private string _rootName = string.Empty;
    #endregion

    public BrowseViewModel(MainViewModel mainViewModel, IFileService fileService, ISettingsService settingsService, IDialogService dialogService, INavigationService navigationService)
    {
        _mainViewModel = mainViewModel;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _appSettings = settingsService.LoadSettings();
        _mainViewModel.LeftTree.CollectionChanged += (s, e) => InitializePaths();
        _mainViewModel.SelectedScanFiles.CollectionChanged += (s, e) => CopyFromScansCommand.NotifyCanExecuteChanged();
        SelectedLeftItems.CollectionChanged += (s, e) => UpdateButtonStates();
        InitializePaths();
    }

    #region Property Changed Handlers
    partial void OnSelectedRightDirectoryChanged(DirectoryItem value) { MoveFileCommand.NotifyCanExecuteChanged(); CopyFromScansCommand.NotifyCanExecuteChanged(); }
    partial void OnSelectedLeftDirectoryChanged(DirectoryItem value) { UpdateLeftBreadcrumbs(value?.FullPath); UpdateAndSortLeftFilteredContents(); }
    partial void OnSelectedRightContentChanged(ContentItem value)
    {
        if (value?.IsFolder == true)
        {
            SelectedRightDirectory = new DirectoryItem { FullPath = value.FullPath, Name = value.Name };
            UpdateRightFilteredContents();
        }
    }
    #endregion

    #region Directory Loading Logic
    private void InitializePaths()
    {
        if (_mainViewModel.LeftTree.Count == 0) return;
        var root = _mainViewModel.LeftTree[0];
        _rootPath = root.FullPath;
        RootName = root.Name;
        SelectedLeftDirectory = root;
        SelectedRightDirectory = root;
        UpdateRightFilteredContents();
    }
    private void UpdateLeftBreadcrumbs(string currentPath) => UpdateBreadcrumbs(currentPath, LeftBreadcrumbs, GoUpLeftCommand, RootName, _rootPath);
    private void UpdateRightBreadcrumbs(string currentPath) => UpdateBreadcrumbs(currentPath, RightBreadcrumbs, GoUpRightCommand, RootName, _rootPath);
    private void UpdateBreadcrumbs(string currentPath, ObservableCollection<BreadcrumbItem> breadcrumbs, IRelayCommand goUpCommand, string rootName, string rootPath)
    {
        breadcrumbs.Clear();
        if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(currentPath) || !currentPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)) return;
        var pathSegments = new List<BreadcrumbItem> { new() { Name = rootName, FullPath = rootPath } };
        if (!currentPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = Path.GetRelativePath(rootPath, currentPath);
            var currentFullPath = rootPath;
            foreach (var part in relativePath.Split(Path.DirectorySeparatorChar)) { currentFullPath = Path.Combine(currentFullPath, part); pathSegments.Add(new BreadcrumbItem { Name = part, FullPath = currentFullPath }); }
        }
        if (pathSegments.Any()) pathSegments.Last().IsLast = true;
        foreach (var segment in pathSegments) breadcrumbs.Add(segment);
        goUpCommand.NotifyCanExecuteChanged();
    }
    private bool DoesContainRelevantFiles(string path)
    {
        try
        {
            var relevantExtensions = new HashSet<string> { ".txt", ".csv" };
            if (Directory.EnumerateFiles(path).Any(f => relevantExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))) return true;
            return Directory.EnumerateDirectories(path).Any(DoesContainRelevantFiles);
        }
        catch { return false; }
    }
    private FileType GetFileType(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".txt" => FileType.Text, ".csv" => FileType.Csv, _ => FileType.Other };
    private void UpdateAndSortLeftFilteredContents()
    {
        var dir = SelectedLeftDirectory;
        if (dir == null) return;
        var tempItems = new List<ContentItem>();
        try
        {
            var subDirs = Directory.GetDirectories(dir.FullPath).Where(DoesContainRelevantFiles).Select(d => new ContentItem { Name = Path.GetFileName(d), FullPath = d, IsFolder = true, ModifiedDate = new DirectoryInfo(d).LastWriteTime });
            tempItems.AddRange(subDirs);
            var relevantExtensions = new HashSet<string> { ".txt", ".csv" };
            var files = Directory.GetFiles(dir.FullPath).Where(f => relevantExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => { var info = new FileInfo(f); return new ContentItem { Name = info.Name, FullPath = info.FullName, IsFolder = false, Size = info.Length, ModifiedDate = info.LastWriteTime }; });
            tempItems.AddRange(files);
        }
        catch (Exception) { /* Handle errors */ }
        Func<ContentItem, object> keySelector = _currentSortColumn switch { "Date" => i => i.ModifiedDate, "Size" => i => i.Size, _ => i => i.Name, };
        var sortedItems = _isSortAscending ? tempItems.OrderBy(i => !i.IsFolder).ThenBy(keySelector) : tempItems.OrderBy(i => !i.IsFolder).ThenByDescending(keySelector);
        LeftFilteredContents.Clear();
        foreach (var item in sortedItems) LeftFilteredContents.Add(item);
    }
    private void UpdateRightFilteredContents()
    {
        RightFilteredContents.Clear();
        var dir = SelectedRightDirectory;
        if (dir == null) return;
        UpdateRightBreadcrumbs(dir.FullPath);
        try
        {
            foreach (var subDirPath in Directory.GetDirectories(dir.FullPath)) RightFilteredContents.Add(new ContentItem { Name = Path.GetFileName(subDirPath), IsFolder = true, FullPath = subDirPath });
            foreach (var file in Directory.GetFiles(dir.FullPath)) RightFilteredContents.Add(new ContentItem { Name = Path.GetFileName(file), IsFolder = false, FullPath = file });
        }
        catch (Exception) { /* Handle errors */ }
    }
    #endregion

    #region CanExecute Predicates & Commands
    private bool CanPerformMultiFileAction() => SelectedLeftItems.Cast<ContentItem>().Any(item => !item.IsFolder);
    private bool CanPerformSingleFileAction() => SelectedLeftItems.Count == 1 && !SelectedLeftItems.Cast<ContentItem>().First().IsFolder;
    private bool CanMoveFile() => CanPerformMultiFileAction() && SelectedRightDirectory != null;
    private bool CanCopyFromScans() => SelectedRightDirectory != null && _mainViewModel.SelectedScanFiles.Any();
    private bool CanUndo() => _undoStack.Any();
    private bool CanGoUpLeft() => LeftBreadcrumbs.Count > 1;
    private bool CanGoUpRight() => RightBreadcrumbs.Count > 1;

    private void UpdateButtonStates()
    {
        RenameFileCommand.NotifyCanExecuteChanged();
        ArchiveFileCommand.NotifyCanExecuteChanged();
        ShipFileCommand.NotifyCanExecuteChanged();
        MoveFileCommand.NotifyCanExecuteChanged();
        NavigateToPreviewCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Sort(string newSortColumn)
    {
        if (string.IsNullOrEmpty(newSortColumn)) return;
        if (_currentSortColumn == newSortColumn) _isSortAscending = !_isSortAscending;
        else { _currentSortColumn = newSortColumn; _isSortAscending = true; }
        UpdateAndSortLeftFilteredContents();
    }

    [RelayCommand(CanExecute = nameof(CanGoUpLeft))]
    private void GoUpLeft()
    {
        var parent = LeftBreadcrumbs.ElementAtOrDefault(LeftBreadcrumbs.Count - 2);
        if (parent != null) SelectedLeftDirectory = new DirectoryItem { FullPath = parent.FullPath, Name = parent.Name };
    }

    [RelayCommand(CanExecute = nameof(CanGoUpRight))]
    private void GoUpRight()
    {
        var parent = RightBreadcrumbs.ElementAtOrDefault(RightBreadcrumbs.Count - 2);
        if (parent != null)
        {
            SelectedRightDirectory = new DirectoryItem { FullPath = parent.FullPath, Name = parent.Name };
            UpdateRightFilteredContents();
        }
    }

    [RelayCommand(CanExecute = nameof(CanPerformSingleFileAction))]
    private async Task RenameFile()
    {
        var fileToRename = SelectedLeftItems.Cast<ContentItem>().First();
        var newName = await _dialogService.ShowRenameDialogAsync(fileToRename.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == fileToRename.Name) return;
        var oldPath = fileToRename.FullPath;
        var newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName);
        try
        {
            File.Move(oldPath, newPath);
            _undoStack.Push(new UndoAction("Rename", new List<FileOp> { new(newPath, oldPath) }));
            UndoCommand.NotifyCanExecuteChanged();
            UpdateAndSortLeftFilteredContents();
        }
        catch (Exception ex) { await _dialogService.ShowErrorDialogAsync("Rename Failed", ex.Message); }
    }

    [RelayCommand(CanExecute = nameof(CanPerformMultiFileAction))]
    private async Task ArchiveFile()
    {
        var filesToMove = SelectedLeftItems.Cast<ContentItem>().Where(i => !i.IsFolder).ToList();
        if (!await _dialogService.ShowConfirmationDialogAsync("Archive Files", $"Move {filesToMove.Count} item(s) to archive?")) return;
        var archivePath = _appSettings.ArchivePath;
        if (string.IsNullOrEmpty(archivePath)) { await _dialogService.ShowErrorDialogAsync("Error", "Archive path is not configured."); return; }
        Directory.CreateDirectory(archivePath);
        var ops = new List<FileOp>();
        foreach (var file in filesToMove)
        {
            var destPath = Path.Combine(archivePath, file.Name);
            try { File.Move(file.FullPath, destPath); ops.Add(new FileOp(destPath, file.FullPath)); }
            catch (Exception ex) { await _dialogService.ShowErrorDialogAsync("Archive Failed", $"Could not archive '{file.Name}': {ex.Message}"); }
        }
        if (ops.Any()) { _undoStack.Push(new UndoAction("Archive", ops)); UndoCommand.NotifyCanExecuteChanged(); UpdateAndSortLeftFilteredContents(); }
    }

    [RelayCommand(CanExecute = nameof(CanPerformMultiFileAction))]
    private async Task ShipFile()
    {
        var filesToMove = SelectedLeftItems.Cast<ContentItem>().Where(i => !i.IsFolder).ToList();
        if (!await _dialogService.ShowConfirmationDialogAsync("Ship Files", $"Move {filesToMove.Count} item(s) to shipped folder?")) return;
        var shippedPath = _appSettings.ShippedDirectory;
        if (string.IsNullOrEmpty(shippedPath)) { await _dialogService.ShowErrorDialogAsync("Error", "Shipped path is not configured."); return; }
        Directory.CreateDirectory(shippedPath);
        var ops = new List<FileOp>();
        foreach (var file in filesToMove)
        {
            var destPath = Path.Combine(shippedPath, file.Name);
            try { File.Move(file.FullPath, destPath); ops.Add(new FileOp(destPath, file.FullPath)); }
            catch (Exception ex) { await _dialogService.ShowErrorDialogAsync("Ship Failed", $"Could not ship '{file.Name}': {ex.Message}"); }
        }
        if (ops.Any()) { _undoStack.Push(new UndoAction("Ship", ops)); UndoCommand.NotifyCanExecuteChanged(); UpdateAndSortLeftFilteredContents(); }
    }

    [RelayCommand(CanExecute = nameof(CanMoveFile))]
    private async Task MoveFile()
    {
        var filesToMove = SelectedLeftItems.Cast<ContentItem>().Where(i => !i.IsFolder).ToList();
        var ops = new List<FileOp>();
        foreach (var file in filesToMove)
        {
            var destPath = Path.Combine(SelectedRightDirectory.FullPath, file.Name);
            try { File.Move(file.FullPath, destPath); ops.Add(new FileOp(destPath, file.FullPath)); }
            catch (Exception ex) { await _dialogService.ShowErrorDialogAsync("Move Failed", $"Could not move '{file.Name}': {ex.Message}"); }
        }
        if (ops.Any()) { _undoStack.Push(new UndoAction("Move", ops)); UndoCommand.NotifyCanExecuteChanged(); UpdateAndSortLeftFilteredContents(); UpdateRightFilteredContents(); }
    }

    [RelayCommand(CanExecute = nameof(CanCopyFromScans))]
    private async Task CopyFromScans()
    {
        foreach (var file in _mainViewModel.SelectedScanFiles)
        {
            try { File.Copy(file.FullPath, Path.Combine(SelectedRightDirectory.FullPath, file.Name), true); }
            catch (Exception ex) { await _dialogService.ShowErrorDialogAsync("Copy Failed", $"Could not copy '{file.Name}'.\nError: {ex.Message}"); }
        }
        UpdateRightFilteredContents();
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task Undo()
    {
        var lastAction = _undoStack.Pop();
        UndoCommand.NotifyCanExecuteChanged();
        foreach (var op in lastAction.Ops)
        {
            try { File.Move(op.FromPath, op.ToPath); }
            catch (Exception ex) { await _dialogService.ShowErrorDialogAsync("Undo Failed", $"Could not move '{Path.GetFileName(op.ToPath)}' back."); }
        }
        UpdateAndSortLeftFilteredContents();
        UpdateRightFilteredContents();
    }

    [RelayCommand(CanExecute = nameof(CanPerformSingleFileAction))]
    private void NavigateToPreview()
    {
        var fileToPreview = SelectedLeftItems.Cast<ContentItem>().First();
        var fileItem = new FileItem { Name = fileToPreview.Name, FullPath = fileToPreview.FullPath, Type = GetFileType(fileToPreview.FullPath) };
        _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
    }

    [RelayCommand]
    private void NavigateLeftBreadcrumb(BreadcrumbItem item)
    {
        if (item != null) SelectedLeftDirectory = new DirectoryItem { FullPath = item.FullPath, Name = item.Name };
    }

    [RelayCommand]
    private void NavigateRightBreadcrumb(BreadcrumbItem item)
    {
        if (item != null)
        {
            SelectedRightDirectory = new DirectoryItem { FullPath = item.FullPath, Name = item.Name };
            UpdateRightFilteredContents();
        }
    }

    // THE FIX: This command now explicitly runs file I/O on a background thread.
    [RelayCommand]
    private async Task MoveFiles(Tuple<IEnumerable<string>, string> dropData)
    {
        if (dropData == null) return;

        var sourceFilePaths = dropData.Item1;
        var destinationFolderPath = dropData.Item2;

        // This will hold the results of the background operation
        List<FileOp> completedOps = null;

        await Task.Run(async () =>
        {
            var ops = new List<FileOp>();
            foreach (var sourcePath in sourceFilePaths)
            {
                var fileName = Path.GetFileName(sourcePath);
                var destPath = Path.Combine(destinationFolderPath, fileName);
                try
                {
                    File.Move(sourcePath, destPath, true); // Overwrite if exists
                    ops.Add(new FileOp(destPath, sourcePath)); // For Undo
                }
                catch (Exception ex)
                {
                    // Can't show a dialog from a background thread directly,
                    // but the failure means the operation won't be added to 'ops'.
                    // Logging would be appropriate here.
                }
            }
            completedOps = ops;
        });

        // This part runs back on the UI thread after the await completes.
        if (completedOps != null && completedOps.Any())
        {
            _undoStack.Push(new UndoAction("Move", completedOps));
            UndoCommand.NotifyCanExecuteChanged();
            UpdateAndSortLeftFilteredContents();
            UpdateRightFilteredContents();
        }
    }
    #endregion
}