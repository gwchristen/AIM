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
    private readonly AuditLoggingService _auditLoggingService;
    private readonly IBrowseStateService _browseStateService;
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

    public BrowseViewModel(MainViewModel mainViewModel, IFileService fileService, ISettingsService settingsService, IDialogService dialogService, INavigationService navigationService, AuditLoggingService auditLoggingService, IBrowseStateService browseStateService)
    {
        _mainViewModel = mainViewModel;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _auditLoggingService = auditLoggingService;
        _appSettings = settingsService.LoadSettings();
        _browseStateService = browseStateService;

        _mainViewModel.LeftTree.CollectionChanged += (s, e) => InitializePaths();
        _mainViewModel.SelectedScanFiles.CollectionChanged += (s, e) => CopyFromScansCommand.NotifyCanExecuteChanged();
        SelectedLeftItems.CollectionChanged += (s, e) => UpdateButtonStates();

        InitializePaths();
        System.Diagnostics.Debug.WriteLine("[BrowseViewModel] Constructor complete, browse state will load in InitializePaths");
    }

    #region Property Changed Handlers
    partial void OnSelectedRightDirectoryChanged(DirectoryItem value)
    {
        if (value != null)
        {
            SelectedRightDirectory = value;
            UpdateRightFilteredContents();
        }
        SaveCurrentBrowseState();

        MoveFileCommand.NotifyCanExecuteChanged();
        CopyFromScansCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedLeftDirectoryChanged(DirectoryItem value)
    {
        if (value != null)
        {
            SelectedLeftDirectory = value;
        }

        _auditLoggingService.LogDirectoryOperation(
            AuditActionTypes.DIR_ACCESS,
            value?.FullPath,
            $"Browsed to directory: {value?.Name}"
        );

        UpdateLeftBreadcrumbs(value?.FullPath);
        UpdateAndSortLeftFilteredContents();
        SaveCurrentBrowseState();
    }

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

        // Load saved browse state AFTER root is initialized
        LoadPreviousBrowseState();
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

    /// <summary>
    /// Load previously saved browse state if available
    /// </summary>
    private void LoadPreviousBrowseState()
    {
        var savedState = _browseStateService.LoadBrowseState();
        if (savedState != null)
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseViewModel] Attempting to load previous browse state");

            // Check if saved left directory exists and is under the root
            if (!string.IsNullOrEmpty(savedState.LeftDirectory)
                && Directory.Exists(savedState.LeftDirectory)
                && savedState.LeftDirectory.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            {
                SelectedLeftDirectory = new DirectoryItem
                {
                    Name = Path.GetFileName(savedState.LeftDirectory),
                    FullPath = savedState.LeftDirectory
                };
                System.Diagnostics.Debug.WriteLine($"[BrowseViewModel] ✓ Restored left directory: {savedState.LeftDirectory}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BrowseViewModel] ✗ Left directory not found or invalid: {savedState.LeftDirectory}");
            }

            // Check if saved right directory exists and is under the root
            if (!string.IsNullOrEmpty(savedState.RightDirectory)
                && Directory.Exists(savedState.RightDirectory)
                && savedState.RightDirectory.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            {
                SelectedRightDirectory = new DirectoryItem
                {
                    Name = Path.GetFileName(savedState.RightDirectory),
                    FullPath = savedState.RightDirectory
                };
                UpdateRightFilteredContents();
                System.Diagnostics.Debug.WriteLine($"[BrowseViewModel] ✓ Restored right directory: {savedState.RightDirectory}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BrowseViewModel] ✗ Right directory not found or invalid: {savedState.RightDirectory}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseViewModel] No saved browse state found");
        }
    }

    /// <summary>
    /// Save current browse state to persistent storage (PUBLIC for explicit calls)
    /// </summary>
    public void SaveCurrentBrowseState()
    {
        var leftDir = SelectedLeftDirectory?.FullPath ?? string.Empty;
        var rightDir = SelectedRightDirectory?.FullPath ?? string.Empty;
        _browseStateService.SaveBrowseState(leftDir, rightDir);
        System.Diagnostics.Debug.WriteLine($"[BrowseViewModel] Browse state saved - Left: {leftDir}, Right: {rightDir}");
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
        if (parent != null)
        {
            SelectedLeftDirectory = new DirectoryItem { FullPath = parent.FullPath, Name = parent.Name };
            SaveCurrentBrowseState();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoUpRight))]
    private void GoUpRight()
    {
        var parent = RightBreadcrumbs.ElementAtOrDefault(RightBreadcrumbs.Count - 2);
        if (parent != null)
        {
            SelectedRightDirectory = new DirectoryItem { FullPath = parent.FullPath, Name = parent.Name };
            UpdateRightFilteredContents();
            SaveCurrentBrowseState();
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

            // Log rename operation with detailed info
            _auditLoggingService.LogRenameOperation(oldPath, fileToRename.Name, newName);

            _undoStack.Push(new UndoAction("Rename", new List<FileOp> { new(newPath, oldPath) }));
            UndoCommand.NotifyCanExecuteChanged();
            UpdateAndSortLeftFilteredContents();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Rename Failed", ex.Message);

            // Log failed rename
            _auditLoggingService.LogFileOperation(
                "FILE_RENAME_FAILED",
                oldPath,
                $"Failed to rename '{fileToRename.Name}' to '{newName}': {ex.Message}",
                new Dictionary<string, string>
                {
                    { "oldName", fileToRename.Name },
                    { "newName", newName },
                    { "error", ex.Message }
                }
            );
        }
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
            try
            {
                File.Move(file.FullPath, destPath);

                // Log archive operation with detailed info
                _auditLoggingService.LogMoveOperation(file.FullPath, destPath, file.Name);

                ops.Add(new FileOp(destPath, file.FullPath));
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync("Archive Failed", $"Could not archive '{file.Name}': {ex.Message}");

                // Log failed archive
                _auditLoggingService.LogFileOperation(
                    "FILE_ARCHIVE_FAILED",
                    file.FullPath,
                    $"Failed to archive '{file.Name}': {ex.Message}",
                    new Dictionary<string, string>
                    {
                        { "destination", destPath },
                        { "error", ex.Message }
                    }
                );
            }
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
            try
            {
                File.Move(file.FullPath, destPath);

                // Log ship operation with detailed info
                _auditLoggingService.LogMoveOperation(file.FullPath, destPath, file.Name);

                ops.Add(new FileOp(destPath, file.FullPath));
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync("Ship Failed", $"Could not ship '{file.Name}': {ex.Message}");

                // Log failed ship
                _auditLoggingService.LogFileOperation(
                    "FILE_SHIP_FAILED",
                    file.FullPath,
                    $"Failed to ship '{file.Name}': {ex.Message}",
                    new Dictionary<string, string>
                    {
                        { "destination", destPath },
                        { "error", ex.Message }
                    }
                );
            }
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
            try
            {
                File.Move(file.FullPath, destPath);

                // Log move operation with detailed info
                _auditLoggingService.LogMoveOperation(file.FullPath, destPath, file.Name);

                ops.Add(new FileOp(destPath, file.FullPath));
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync("Move Failed", $"Could not move '{file.Name}': {ex.Message}");

                // Log failed move
                _auditLoggingService.LogFileOperation(
                    "FILE_MOVE_FAILED",
                    file.FullPath,
                    $"Failed to move '{file.Name}': {ex.Message}",
                    new Dictionary<string, string>
                    {
                        { "destination", destPath },
                        { "error", ex.Message }
                    }
                );
            }
        }
        if (ops.Any()) { _undoStack.Push(new UndoAction("Move", ops)); UndoCommand.NotifyCanExecuteChanged(); UpdateAndSortLeftFilteredContents(); UpdateRightFilteredContents(); }
    }

    [RelayCommand(CanExecute = nameof(CanCopyFromScans))]
    private async Task CopyFromScans()
    {
        foreach (var file in _mainViewModel.SelectedScanFiles)
        {
            try
            {
                var destPath = Path.Combine(SelectedRightDirectory.FullPath, file.Name);
                File.Copy(file.FullPath, destPath, true);

                // Log copy operation with detailed info
                _auditLoggingService.LogCopyOperation(file.FullPath, destPath, file.Name);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync("Copy Failed", $"Could not copy '{file.Name}'.\nError: {ex.Message}");

                // Log failed copy
                _auditLoggingService.LogFileOperation(
                    "FILE_COPY_FAILED",
                    file.FullPath,
                    $"Failed to copy '{file.Name}' from Scans: {ex.Message}",
                    new Dictionary<string, string>
                    {
                        { "destination", Path.Combine(SelectedRightDirectory.FullPath, file.Name) },
                        { "error", ex.Message }
                    }
                );
            }
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
            try
            {
                File.Move(op.FromPath, op.ToPath);

                // Log undo operation
                _auditLoggingService.LogAction(new AuditLogEntry
                {
                    ActionType = "ACTION_UNDONE",
                    Description = $"Undid {lastAction.Type} operation",
                    TargetPath = op.FromPath,
                    UserId = Environment.UserName,
                    Details = $"Moved from {op.FromPath} back to {op.ToPath}"
                });
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync("Undo Failed", $"Could not move '{Path.GetFileName(op.ToPath)}' back.");

                // Log failed undo
                _auditLoggingService.LogFileOperation(
                    "UNDO_FAILED",
                    op.FromPath,
                    $"Failed to undo {lastAction.Type} operation: {ex.Message}",
                    new Dictionary<string, string>
                    {
                        { "originalPath", op.ToPath },
                        { "error", ex.Message }
                    }
                );
            }
        }
        UpdateAndSortLeftFilteredContents();
        UpdateRightFilteredContents();
    }

    [RelayCommand(CanExecute = nameof(CanPerformSingleFileAction))]
    private void NavigateToPreview()
    {
        var fileToPreview = SelectedLeftItems.Cast<ContentItem>().First();
        var fileItem = new FileItem { Name = fileToPreview.Name, FullPath = fileToPreview.FullPath, Type = GetFileType(fileToPreview.FullPath) };

        // Log preview navigation
        _auditLoggingService.LogPreviewOperation(fileToPreview.FullPath, fileToPreview.Name);

        _navigationService.NavigateTo(typeof(PreviewPage), fileItem, "Preview");
    }

    [RelayCommand]
    private void NavigateLeftBreadcrumb(BreadcrumbItem item)
    {
        if (item != null)
        {
            SelectedLeftDirectory = new DirectoryItem { FullPath = item.FullPath, Name = item.Name };
            SaveCurrentBrowseState();
        }
    }

    [RelayCommand]
    private void NavigateRightBreadcrumb(BreadcrumbItem item)
    {
        if (item != null)
        {
            SelectedRightDirectory = new DirectoryItem { FullPath = item.FullPath, Name = item.Name };
            UpdateRightFilteredContents();
            SaveCurrentBrowseState();
        }
    }

    [RelayCommand]
    private async Task MoveFiles(Tuple<IEnumerable<string>, string> dropData)
    {
        if (dropData == null) return;

        var sourceFilePaths = dropData.Item1;
        var destinationFolderPath = dropData.Item2;

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
                    File.Move(sourcePath, destPath, true);

                    // Log drag-drop move operation
                    _auditLoggingService.LogMoveOperation(sourcePath, destPath, fileName);

                    ops.Add(new FileOp(destPath, sourcePath));
                }
                catch (Exception ex)
                {
                    // Log failed drag-drop move
                    _auditLoggingService.LogFileOperation(
                        "FILE_MOVE_FAILED",
                        sourcePath,
                        $"Failed to move '{fileName}' via drag-drop: {ex.Message}",
                        new Dictionary<string, string>
                        {
                            { "destination", destPath },
                            { "error", ex.Message }
                        }
                    );
                }
            }
            completedOps = ops;
        });

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