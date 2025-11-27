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

    [ObservableProperty]
    private ObservableCollection<object> _selectedLeftItems = new();

    public ObservableCollection<string> PersistentSelectedPaths { get; } = new();
    #endregion

    #region Observable Properties
    [ObservableProperty]
    private ContentItem _selectedRightContent;

    [ObservableProperty]
    private DirectoryItem _selectedLeftDirectory;

    [ObservableProperty]
    private DirectoryItem _selectedRightDirectory;

    [ObservableProperty]
    private string _rootName = string.Empty;

    [ObservableProperty]
    private int _selectedFileCount;

    [ObservableProperty]
    private int _selectedFolderCount;

    [ObservableProperty]
    private string _selectionStatusText = "No items selected";

    [ObservableProperty]
    private string _operationStatusText = string.Empty;

    [ObservableProperty]
    private bool _isOperationInProgress;

    [ObservableProperty]
    private int _undoStackCount;
    #endregion


    #region Panel States
    // Loading states
    [ObservableProperty]
    private bool _isLeftPanelLoading;

    [ObservableProperty]
    private bool _isRightPanelLoading;

    [ObservableProperty]
    private string _leftPanelLoadingText = "Loading... ";

    [ObservableProperty]
    private string _rightPanelLoadingText = "Loading... ";
    #endregion

    #region Error Handling
    // Error states
    [ObservableProperty]
    private bool _hasLeftPanelError;

    [ObservableProperty]
    private string _leftPanelErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasRightPanelError;

    [ObservableProperty]
    private string _rightPanelErrorMessage = string.Empty;
    #endregion


    public BrowseViewModel(MainViewModel mainViewModel, IFileService fileService, ISettingsService settingsService, IDialogService dialogService, INavigationService navigationService, AuditLoggingService auditLoggingService)
    {
        _mainViewModel = mainViewModel;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _auditLoggingService = auditLoggingService;
        _appSettings = settingsService.LoadSettings();
        _mainViewModel.LeftTree.CollectionChanged += (s, e) => InitializePaths();
        _mainViewModel.SelectedScanFiles.CollectionChanged += (s, e) => CopyFromScansCommand.NotifyCanExecuteChanged();
        SelectedLeftItems.CollectionChanged += (s, e) => OnLocalSelectionChanged();
        PersistentSelectedPaths.CollectionChanged += (s, e) =>
        {
            UpdateSelectionStatus();
            UpdateButtonStates();
        };
        InitializePaths();
        UpdateButtonStates();
    }

    #region Selection Management

    private void OnLocalSelectionChanged()
    {
        UpdateSelectionStatus();
        UpdateButtonStates();
    }

    public void AddToPersistentSelection(ContentItem item)
    {
        if (item != null && !item.IsFolder && !PersistentSelectedPaths.Contains(item.FullPath))
        {
            PersistentSelectedPaths.Add(item.FullPath);
        }
    }

    public void RemoveFromPersistentSelection(ContentItem item)
    {
        if (item != null)
        {
            PersistentSelectedPaths.Remove(item.FullPath);
        }
    }

    public void TogglePersistentSelection(ContentItem item)
    {
        if (item == null || item.IsFolder) return;

        if (PersistentSelectedPaths.Contains(item.FullPath))
        {
            PersistentSelectedPaths.Remove(item.FullPath);
        }
        else
        {
            PersistentSelectedPaths.Add(item.FullPath);
        }
    }

    public IEnumerable<ContentItem> GetPersistentSelectedFiles()
    {
        return PersistentSelectedPaths
            .Where(File.Exists)
            .Select(path => new ContentItem
            {
                Name = Path.GetFileName(path),
                FullPath = path,
                IsFolder = false,
                Size = new FileInfo(path).Length,
                ModifiedDate = new FileInfo(path).LastWriteTime
            });
    }

    public bool IsInPersistentSelection(string fullPath)
    {
        return PersistentSelectedPaths.Contains(fullPath);
    }

    private void UpdateSelectionStatus()
    {
        var persistentCount = PersistentSelectedPaths.Count;
        var localFiles = SelectedLeftItems.Cast<ContentItem>().Count(i => !i.IsFolder);
        var localFolders = SelectedLeftItems.Cast<ContentItem>().Count(i => i.IsFolder);

        SelectedFileCount = persistentCount > 0 ? persistentCount : localFiles;
        SelectedFolderCount = localFolders;

        if (persistentCount > 0)
        {
            var dirCount = PersistentSelectedPaths.Select(p => Path.GetDirectoryName(p)).Distinct().Count();
            SelectionStatusText = $"{persistentCount} file(s) selected from {dirCount} folder(s)";
        }
        else if (localFiles > 0 || localFolders > 0)
        {
            var parts = new List<string>();
            if (localFiles > 0) parts.Add($"{localFiles} file(s)");
            if (localFolders > 0) parts.Add($"{localFolders} folder(s)");
            SelectionStatusText = string.Join(", ", parts) + " selected";
        }
        else
        {
            SelectionStatusText = "No items selected";
        }
    }

    private async void SetOperationStatus(string message, bool autoClear = true, int delayMs = 3000)
    {
        OperationStatusText = message;

        if (autoClear)
        {
            await Task.Delay(delayMs);
            if (OperationStatusText == message)
            {
                OperationStatusText = string.Empty;
            }
        }
    }

    public void SetOperationStatusPublic(string message, bool autoClear = true, int delayMs = 3000)
    {
        SetOperationStatus(message, autoClear, delayMs);
    }

    private string GetRelativePathForDisplay(string fullPath)
    {
        if (string.IsNullOrEmpty(_rootPath) || string.IsNullOrEmpty(fullPath))
            return fullPath ?? "";

        if (fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(_rootPath, fullPath);
            return string.IsNullOrEmpty(relative) || relative == "." ? RootName : $"{RootName}\\{relative}";
        }

        return fullPath;
    }

    #endregion

    #region Property Changed Handlers
    partial void OnSelectedRightDirectoryChanged(DirectoryItem value)
    {
        MoveFileCommand.NotifyCanExecuteChanged();
        CopyFromScansCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedLeftDirectoryChanged(DirectoryItem value)
    {
        _auditLoggingService.LogDirectoryOperation(
            AuditActionTypes.DIR_ACCESS,
            value?.FullPath,
            $"Browsed to directory: {value?.Name}"
        );

        UpdateLeftBreadcrumbs(value?.FullPath);
        _ = UpdateAndSortLeftFilteredContentsAsync();
    }

    partial void OnSelectedRightContentChanged(ContentItem value)
    {
        if (value?.IsFolder == true)
        {
            SelectedRightDirectory = new DirectoryItem { FullPath = value.FullPath, Name = value.Name };
            _ = UpdateRightFilteredContentsAsync();
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
        _ = UpdateRightFilteredContentsAsync();
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
            foreach (var part in relativePath.Split(Path.DirectorySeparatorChar))
            {
                currentFullPath = Path.Combine(currentFullPath, part);
                pathSegments.Add(new BreadcrumbItem { Name = part, FullPath = currentFullPath });
            }
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

    private FileType GetFileType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".txt" => FileType.Text,
        ".csv" => FileType.Csv,
        _ => FileType.Other
    };

    private async Task UpdateAndSortLeftFilteredContentsAsync()
    {
        var dir = SelectedLeftDirectory;
        if (dir == null) return;

        HasLeftPanelError = false;
        LeftPanelErrorMessage = string.Empty;
        IsLeftPanelLoading = true;
        LeftPanelLoadingText = $"Loading {dir.Name}... ";

        try
        {
            var tempItems = new List<ContentItem>();

            await Task.Run(() =>
            {
                try
                {
                    var subDirs = Directory.GetDirectories(dir.FullPath)
                        .Where(DoesContainRelevantFiles)
                        .Select(d => new ContentItem
                        {
                            Name = Path.GetFileName(d),
                            FullPath = d,
                            IsFolder = true,
                            ModifiedDate = Directory.GetLastWriteTime(d)
                        });
                    tempItems.AddRange(subDirs);

                    var relevantExtensions = new HashSet<string> { ".txt", ".csv" };
                    var files = Directory.GetFiles(dir.FullPath)
                        .Where(f => relevantExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .Select(f =>
                        {
                            var info = new FileInfo(f);
                            return new ContentItem
                            {
                                Name = info.Name,
                                FullPath = info.FullName,
                                IsFolder = false,
                                Size = info.Length,
                                ModifiedDate = info.LastWriteTime
                            };
                        });
                    tempItems.AddRange(files);
                }
                catch (UnauthorizedAccessException)
                {
                    throw new Exception($"Access denied to '{dir.Name}'. You don't have permission to view this folder.");
                }
                catch (DirectoryNotFoundException)
                {
                    throw new Exception($"Folder '{dir.Name}' no longer exists.  It may have been moved or deleted.");
                }
                catch (IOException ex)
                {
                    throw new Exception($"Unable to read '{dir.Name}': {ex.Message}");
                }
            });

            Func<ContentItem, object> keySelector = _currentSortColumn switch
            {
                "Date" => i => i.ModifiedDate,
                "Size" => i => i.Size,
                _ => i => i.Name
            };
            var sortedItems = _isSortAscending
                ? tempItems.OrderBy(i => !i.IsFolder).ThenBy(keySelector)
                : tempItems.OrderBy(i => !i.IsFolder).ThenByDescending(keySelector);

            LeftFilteredContents.Clear();
            foreach (var item in sortedItems) LeftFilteredContents.Add(item);

            UpdateSelectionStatus();
        }
        catch (Exception ex)
        {
            HasLeftPanelError = true;
            LeftPanelErrorMessage = ex.Message;
            LeftFilteredContents.Clear();
        }
        finally
        {
            IsLeftPanelLoading = false;
        }
    }

    private async Task UpdateRightFilteredContentsAsync()
    {
        var dir = SelectedRightDirectory;
        if (dir == null) return;

        HasRightPanelError = false;
        RightPanelErrorMessage = string.Empty;
        IsRightPanelLoading = true;
        RightPanelLoadingText = $"Loading {dir.Name}...";

        UpdateRightBreadcrumbs(dir.FullPath);

        try
        {
            var tempItems = new List<ContentItem>();

            await Task.Run(() =>
            {
                try
                {
                    foreach (var subDirPath in Directory.GetDirectories(dir.FullPath))
                        tempItems.Add(new ContentItem { Name = Path.GetFileName(subDirPath), IsFolder = true, FullPath = subDirPath });
                    foreach (var file in Directory.GetFiles(dir.FullPath))
                        tempItems.Add(new ContentItem { Name = Path.GetFileName(file), IsFolder = false, FullPath = file });
                }
                catch (UnauthorizedAccessException)
                {
                    throw new Exception($"Access denied to '{dir.Name}'. You don't have permission to view this folder.");
                }
                catch (DirectoryNotFoundException)
                {
                    throw new Exception($"Folder '{dir.Name}' no longer exists. It may have been moved or deleted.");
                }
                catch (IOException ex)
                {
                    throw new Exception($"Unable to read '{dir.Name}': {ex.Message}");
                }
            });

            RightFilteredContents.Clear();
            foreach (var item in tempItems) RightFilteredContents.Add(item);
        }
        catch (Exception ex)
        {
            HasRightPanelError = true;
            RightPanelErrorMessage = ex.Message;
            RightFilteredContents.Clear();
        }
        finally
        {
            IsRightPanelLoading = false;
        }
    }

    [RelayCommand]
    private async Task RetryLeftPanel()
    {
        await UpdateAndSortLeftFilteredContentsAsync();
    }

    [RelayCommand]
    private async Task RetryRightPanel()
    {
        await UpdateRightFilteredContentsAsync();
    }
    #endregion

    #region CanExecute Predicates & Commands
    private bool HasSelectedFiles()
    {
        if (PersistentSelectedPaths.Any()) return true;
        return SelectedLeftItems.Cast<ContentItem>().Any(item => !item.IsFolder);
    }

    private bool CanPerformMultiFileAction() => HasSelectedFiles();

    private bool CanPerformSingleFileAction()
    {
        return SelectedLeftItems.Count == 1 && !SelectedLeftItems.Cast<ContentItem>().First().IsFolder;
    }

    private bool CanMoveFile() => HasSelectedFiles() && SelectedRightDirectory != null;

    private bool CanCopyFromScans()
    {
        var hasRightDir = SelectedRightDirectory != null;
        var hasScanFiles = _mainViewModel.SelectedScanFiles.Any();
        return hasRightDir && hasScanFiles;
    }

    private bool CanUndo() => _undoStack.Any();
    private bool CanGoUpLeft() => LeftBreadcrumbs.Count > 1;
    private bool CanGoUpRight() => RightBreadcrumbs.Count > 1;
    private bool CanAddToSelection() => SelectedLeftItems.Cast<ContentItem>().Any(i => !i.IsFolder);
    private bool HasPersistentSelections() => PersistentSelectedPaths.Any();

    private void UpdateButtonStates()
    {
        RenameFileCommand.NotifyCanExecuteChanged();
        ArchiveFileCommand.NotifyCanExecuteChanged();
        ShipFileCommand.NotifyCanExecuteChanged();
        MoveFileCommand.NotifyCanExecuteChanged();
        NavigateToPreviewCommand.NotifyCanExecuteChanged();
        CopyFromScansCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        AddToSelectionCommand.NotifyCanExecuteChanged();
        PreviewSelectionCommand.NotifyCanExecuteChanged();
        UndoStackCount = _undoStack.Count;
    }

    private List<ContentItem> GetFilesToOperate()
    {
        if (PersistentSelectedPaths.Any())
        {
            return GetPersistentSelectedFiles().ToList();
        }
        return SelectedLeftItems.Cast<ContentItem>().Where(i => !i.IsFolder).ToList();
    }

    /// <summary>
    /// Builds a detailed confirmation message for file operations
    /// </summary>
    private string BuildConfirmationMessage(string operation, List<ContentItem> files, string destination = null)
    {
        var message = new List<string>();

        message.Add($"You are about to {operation.ToLower()} {files.Count} file(s):");
        message.Add("");

        // Show up to 5 files
        var displayFiles = files.Take(5).ToList();
        foreach (var file in displayFiles)
        {
            var folder = GetRelativePathForDisplay(Path.GetDirectoryName(file.FullPath));
            message.Add($"  • {file.Name}");
            message.Add($"    From: {folder}");
        }

        if (files.Count > 5)
        {
            message.Add($"  ... and {files.Count - 5} more file(s)");
        }

        if (!string.IsNullOrEmpty(destination))
        {
            message.Add("");
            message.Add($"Destination: {destination}");
        }

        message.Add("");
        message.Add("Do you want to continue?");

        return string.Join("\n", message);
    }
    #endregion

    #region Commands
    [RelayCommand]
    private void Sort(string newSortColumn)
    {
        if (string.IsNullOrEmpty(newSortColumn)) return;
        if (_currentSortColumn == newSortColumn) _isSortAscending = !_isSortAscending;
        else { _currentSortColumn = newSortColumn; _isSortAscending = true; }
        _ = UpdateAndSortLeftFilteredContentsAsync();
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
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        PersistentSelectedPaths.Clear();
        SelectedLeftItems.Clear();
        UpdateSelectionStatus();
        SetOperationStatus("Selection cleared");
    }

    [RelayCommand(CanExecute = nameof(CanAddToSelection))]
    private void AddToSelection()
    {
        int addedCount = 0;
        foreach (var item in SelectedLeftItems.Cast<ContentItem>())
        {
            if (!item.IsFolder && !PersistentSelectedPaths.Contains(item.FullPath))
            {
                PersistentSelectedPaths.Add(item.FullPath);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            SetOperationStatus($"Added {addedCount} file(s) to selection");
        }
        else
        {
            SetOperationStatus("Files already in selection");
        }
    }

    [RelayCommand(CanExecute = nameof(HasPersistentSelections))]
    private async Task PreviewSelection()
    {
        var files = GetPersistentSelectedFiles().ToList();

        if (!files.Any())
        {
            await _dialogService.ShowErrorDialogAsync("No Selection", "No files are currently selected.");
            return;
        }

        var groupedFiles = files
            .GroupBy(f => Path.GetDirectoryName(f.FullPath))
            .OrderBy(g => g.Key);

        var messageLines = new List<string>
        {
            $"{files.Count} file(s) selected:",
            ""
        };

        foreach (var group in groupedFiles)
        {
            var relativePath = GetRelativePathForDisplay(group.Key);
            messageLines.Add($"📁 {relativePath}");
            foreach (var file in group.OrderBy(f => f.Name))
            {
                messageLines.Add($"    📄 {file.Name}");
            }
            messageLines.Add("");
        }

        await _dialogService.ShowInfoDialogAsync("Selection Preview", string.Join("\n", messageLines));
    }

    [RelayCommand(CanExecute = nameof(CanPerformSingleFileAction))]
    private async Task RenameFile()
    {
        var fileToRename = SelectedLeftItems.Cast<ContentItem>().First();
        var newName = await _dialogService.ShowRenameDialogAsync(fileToRename.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == fileToRename.Name) return;

        var oldPath = fileToRename.FullPath;
        var directory = Path.GetDirectoryName(oldPath)!;
        var newPath = Path.Combine(directory, newName);

        // Validate new name
        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            await _dialogService.ShowErrorDialogAsync("Invalid Name",
                "The file name contains invalid characters.  Please use a different name.");
            return;
        }

        if (File.Exists(newPath))
        {
            await _dialogService.ShowErrorDialogAsync("File Exists",
                $"A file named '{newName}' already exists in this location. Please use a different name.");
            return;
        }

        try
        {
            IsOperationInProgress = true;
            SetOperationStatus($"Renaming '{fileToRename.Name}'.. .", false);

            File.Move(oldPath, newPath);
            _auditLoggingService.LogRenameOperation(oldPath, fileToRename.Name, newName);
            _undoStack.Push(new UndoAction("Rename", new List<FileOp> { new(newPath, oldPath) }));
            UndoCommand.NotifyCanExecuteChanged();
            await UpdateAndSortLeftFilteredContentsAsync();

            SetOperationStatus($"Renamed '{fileToRename.Name}' to '{newName}'");
        }
        catch (UnauthorizedAccessException)
        {
            await _dialogService.ShowErrorDialogAsync("Access Denied",
                $"You don't have permission to rename '{fileToRename.Name}'. The file may be read-only or in use.");
            SetOperationStatus("Rename failed - access denied");
        }
        catch (IOException ex) when (ex.Message.Contains("being used"))
        {
            await _dialogService.ShowErrorDialogAsync("File In Use",
                $"Cannot rename '{fileToRename.Name}' because it is currently open in another program. Please close the file and try again.");
            SetOperationStatus("Rename failed - file in use");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Rename Failed",
                $"Could not rename '{fileToRename.Name}'.\n\nError: {ex.Message}");
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
            SetOperationStatus($"Failed to rename '{fileToRename.Name}'");
        }
        finally
        {
            IsOperationInProgress = false;
            UpdateButtonStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanPerformMultiFileAction))]
    private async Task ArchiveFile()
    {
        var filesToMove = GetFilesToOperate();
        var archivePath = _appSettings.ArchivePath;

        if (string.IsNullOrEmpty(archivePath))
        {
            await _dialogService.ShowErrorDialogAsync("Not Configured",
                "Archive path is not configured. Please set the archive folder in Settings.");
            return;
        }

        var confirmMessage = BuildConfirmationMessage("Archive", filesToMove, archivePath);
        if (!await _dialogService.ShowConfirmationDialogAsync("Archive Files", confirmMessage)) return;

        Directory.CreateDirectory(archivePath);
        var ops = new List<FileOp>();
        var successCount = 0;
        var failedFiles = new List<(string Name, string Error)>();

        try
        {
            IsOperationInProgress = true;

            foreach (var file in filesToMove)
            {
                SetOperationStatus($"Archiving '{file.Name}'...  ({successCount + 1}/{filesToMove.Count})", false);
                var destPath = Path.Combine(archivePath, file.Name);

                try
                {
                    if (File.Exists(destPath))
                    {
                        // Generate unique name
                        var baseName = Path.GetFileNameWithoutExtension(file.Name);
                        var ext = Path.GetExtension(file.Name);
                        var counter = 1;
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(archivePath, $"{baseName} ({counter}){ext}");
                            counter++;
                        }
                    }

                    File.Move(file.FullPath, destPath);
                    _auditLoggingService.LogMoveOperation(file.FullPath, destPath, file.Name);
                    ops.Add(new FileOp(destPath, file.FullPath));
                    PersistentSelectedPaths.Remove(file.FullPath);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedFiles.Add((file.Name, ex.Message));
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

            if (ops.Any())
            {
                _undoStack.Push(new UndoAction("Archive", ops));
                UndoCommand.NotifyCanExecuteChanged();
                await UpdateAndSortLeftFilteredContentsAsync();
            }

            if (failedFiles.Any())
            {
                var errorMessage = $"Archived {successCount} of {filesToMove.Count} file(s).\n\nFailed files:\n" +
                    string.Join("\n", failedFiles.Select(f => $"  • {f.Name}: {f.Error}"));
                await _dialogService.ShowErrorDialogAsync("Partial Success", errorMessage);
            }

            SetOperationStatus($"Archived {successCount} file(s)");
        }
        finally
        {
            IsOperationInProgress = false;
            UpdateButtonStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanPerformMultiFileAction))]
    private async Task ShipFile()
    {
        var filesToMove = GetFilesToOperate();
        var shippedPath = _appSettings.ShippedDirectory;

        if (string.IsNullOrEmpty(shippedPath))
        {
            await _dialogService.ShowErrorDialogAsync("Not Configured",
                "Shipped folder path is not configured. Please set the shipped folder in Settings.");
            return;
        }

        var confirmMessage = BuildConfirmationMessage("Ship", filesToMove, shippedPath);
        if (!await _dialogService.ShowConfirmationDialogAsync("Ship Files", confirmMessage)) return;

        Directory.CreateDirectory(shippedPath);
        var ops = new List<FileOp>();
        var successCount = 0;
        var failedFiles = new List<(string Name, string Error)>();

        try
        {
            IsOperationInProgress = true;

            foreach (var file in filesToMove)
            {
                SetOperationStatus($"Shipping '{file.Name}'... ({successCount + 1}/{filesToMove.Count})", false);
                var destPath = Path.Combine(shippedPath, file.Name);

                try
                {
                    if (File.Exists(destPath))
                    {
                        var baseName = Path.GetFileNameWithoutExtension(file.Name);
                        var ext = Path.GetExtension(file.Name);
                        var counter = 1;
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(shippedPath, $"{baseName} ({counter}){ext}");
                            counter++;
                        }
                    }

                    File.Move(file.FullPath, destPath);
                    _auditLoggingService.LogMoveOperation(file.FullPath, destPath, file.Name);
                    ops.Add(new FileOp(destPath, file.FullPath));
                    PersistentSelectedPaths.Remove(file.FullPath);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedFiles.Add((file.Name, ex.Message));
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

            if (ops.Any())
            {
                _undoStack.Push(new UndoAction("Ship", ops));
                UndoCommand.NotifyCanExecuteChanged();
                await UpdateAndSortLeftFilteredContentsAsync();
            }

            if (failedFiles.Any())
            {
                var errorMessage = $"Shipped {successCount} of {filesToMove.Count} file(s).\n\nFailed files:\n" +
                    string.Join("\n", failedFiles.Select(f => $"  • {f.Name}: {f.Error}"));
                await _dialogService.ShowErrorDialogAsync("Partial Success", errorMessage);
            }

            SetOperationStatus($"Shipped {successCount} file(s)");
        }
        finally
        {
            IsOperationInProgress = false;
            UpdateButtonStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveFile))]
    private async Task MoveFile()
    {
        var filesToMove = GetFilesToOperate();
        var destination = SelectedRightDirectory!.FullPath;
        var destinationName = GetRelativePathForDisplay(destination);

        var confirmMessage = BuildConfirmationMessage("Move", filesToMove, destinationName);
        if (!await _dialogService.ShowConfirmationDialogAsync("Move Files", confirmMessage)) return;

        var ops = new List<FileOp>();
        var successCount = 0;
        var failedFiles = new List<(string Name, string Error)>();

        try
        {
            IsOperationInProgress = true;

            foreach (var file in filesToMove)
            {
                SetOperationStatus($"Moving '{file.Name}'...  ({successCount + 1}/{filesToMove.Count})", false);
                var destPath = Path.Combine(destination, file.Name);

                try
                {
                    if (File.Exists(destPath))
                    {
                        var baseName = Path.GetFileNameWithoutExtension(file.Name);
                        var ext = Path.GetExtension(file.Name);
                        var counter = 1;
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(destination, $"{baseName} ({counter}){ext}");
                            counter++;
                        }
                    }

                    File.Move(file.FullPath, destPath);
                    _auditLoggingService.LogMoveOperation(file.FullPath, destPath, file.Name);
                    ops.Add(new FileOp(destPath, file.FullPath));
                    PersistentSelectedPaths.Remove(file.FullPath);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedFiles.Add((file.Name, ex.Message));
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

            if (ops.Any())
            {
                _undoStack.Push(new UndoAction("Move", ops));
                UndoCommand.NotifyCanExecuteChanged();
                await UpdateAndSortLeftFilteredContentsAsync();
                await UpdateRightFilteredContentsAsync();
            }

            if (failedFiles.Any())
            {
                var errorMessage = $"Moved {successCount} of {filesToMove.Count} file(s).\n\nFailed files:\n" +
                    string.Join("\n", failedFiles.Select(f => $"  • {f.Name}: {f.Error}"));
                await _dialogService.ShowErrorDialogAsync("Partial Success", errorMessage);
            }

            SetOperationStatus($"Moved {successCount} file(s) to {SelectedRightDirectory!.Name}");
        }
        finally
        {
            IsOperationInProgress = false;
            UpdateButtonStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyFromScans))]
    private async Task CopyFromScans()
    {
        var files = _mainViewModel.SelectedScanFiles.ToList();
        var destination = SelectedRightDirectory!.FullPath;
        var destinationName = GetRelativePathForDisplay(destination);

        var confirmMessage = $"Copy {files.Count} file(s) from Scans to:\n{destinationName}\n\nDo you want to continue?";
        if (!await _dialogService.ShowConfirmationDialogAsync("Copy from Scans", confirmMessage)) return;

        var successCount = 0;
        var failedFiles = new List<(string Name, string Error)>();

        try
        {
            IsOperationInProgress = true;

            foreach (var file in files)
            {
                SetOperationStatus($"Copying '{file.Name}'... ({successCount + 1}/{files.Count})", false);
                try
                {
                    var destPath = Path.Combine(destination, file.Name);
                    File.Copy(file.FullPath, destPath, true);
                    _auditLoggingService.LogCopyOperation(file.FullPath, destPath, file.Name);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedFiles.Add((file.Name, ex.Message));
                    _auditLoggingService.LogFileOperation(
                        "FILE_COPY_FAILED",
                        file.FullPath,
                        $"Failed to copy '{file.Name}' from Scans: {ex.Message}",
                        new Dictionary<string, string>
                        {
                            { "destination", Path.Combine(destination, file.Name) },
                            { "error", ex. Message }
                        }
                    );
                }
            }

            _mainViewModel.SelectedScanFiles.Clear();
            await UpdateRightFilteredContentsAsync();
            CopyFromScansCommand.NotifyCanExecuteChanged();

            if (failedFiles.Any())
            {
                var errorMessage = $"Copied {successCount} of {files.Count} file(s).\n\nFailed files:\n" +
                    string.Join("\n", failedFiles.Select(f => $"  • {f.Name}: {f.Error}"));
                await _dialogService.ShowErrorDialogAsync("Partial Success", errorMessage);
            }

            SetOperationStatus($"Copied {successCount} file(s) from Scans");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task Undo()
    {
        var lastAction = _undoStack.Pop();
        UndoCommand.NotifyCanExecuteChanged();

        var confirmMessage = $"Undo {lastAction.Type} operation?\n\nThis will restore {lastAction.Ops.Count} file(s) to their original locations.";
        if (!await _dialogService.ShowConfirmationDialogAsync("Undo", confirmMessage))
        {
            _undoStack.Push(lastAction); // Put it back
            UndoCommand.NotifyCanExecuteChanged();
            return;
        }

        var successCount = 0;
        var failedFiles = new List<(string Name, string Error)>();

        try
        {
            IsOperationInProgress = true;

            foreach (var op in lastAction.Ops)
            {
                SetOperationStatus($"Undoing {lastAction.Type}...  ({successCount + 1}/{lastAction.Ops.Count})", false);
                try
                {
                    File.Move(op.FromPath, op.ToPath);
                    _auditLoggingService.LogAction(new AuditLogEntry
                    {
                        ActionType = "ACTION_UNDONE",
                        Description = $"Undid {lastAction.Type} operation",
                        TargetPath = op.FromPath,
                        UserId = Environment.UserName,
                        Details = $"Moved from {op.FromPath} back to {op.ToPath}"
                    });
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedFiles.Add((Path.GetFileName(op.ToPath), ex.Message));
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

            await UpdateAndSortLeftFilteredContentsAsync();
            await UpdateRightFilteredContentsAsync();

            if (failedFiles.Any())
            {
                var errorMessage = $"Restored {successCount} of {lastAction.Ops.Count} file(s).\n\nFailed files:\n" +
                    string.Join("\n", failedFiles.Select(f => $"  • {f.Name}: {f.Error}"));
                await _dialogService.ShowErrorDialogAsync("Partial Undo", errorMessage);
            }

            SetOperationStatus($"Undid {lastAction.Type} ({successCount} file(s))");
        }
        finally
        {
            IsOperationInProgress = false;
            UpdateButtonStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanPerformSingleFileAction))]
    private void NavigateToPreview()
    {
        var fileToPreview = SelectedLeftItems.Cast<ContentItem>().First();
        var fileItem = new FileItem { Name = fileToPreview.Name, FullPath = fileToPreview.FullPath, Type = GetFileType(fileToPreview.FullPath) };
        _auditLoggingService.LogPreviewOperation(fileToPreview.FullPath, fileToPreview.Name);
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
        }
    }

    [RelayCommand]
    private async Task MoveFiles(Tuple<IEnumerable<string>, string> dropData)
    {
        if (dropData == null) return;

        var sourceFilePaths = dropData.Item1.ToList();
        var destinationFolderPath = dropData.Item2;
        List<FileOp>? completedOps = null;

        try
        {
            IsOperationInProgress = true;
            SetOperationStatus($"Moving {sourceFilePaths.Count} file(s)...", false);

            await Task.Run(() =>
            {
                var ops = new List<FileOp>();
                foreach (var sourcePath in sourceFilePaths)
                {
                    var fileName = Path.GetFileName(sourcePath);
                    var destPath = Path.Combine(destinationFolderPath, fileName);
                    try
                    {
                        if (File.Exists(destPath))
                        {
                            var baseName = Path.GetFileNameWithoutExtension(fileName);
                            var ext = Path.GetExtension(fileName);
                            var counter = 1;
                            while (File.Exists(destPath))
                            {
                                destPath = Path.Combine(destinationFolderPath, $"{baseName} ({counter}){ext}");
                                counter++;
                            }
                        }

                        File.Move(sourcePath, destPath);
                        _auditLoggingService.LogMoveOperation(sourcePath, destPath, fileName);
                        ops.Add(new FileOp(destPath, sourcePath));
                    }
                    catch (Exception ex)
                    {
                        _auditLoggingService.LogFileOperation(
                            "FILE_MOVE_FAILED",
                            sourcePath,
                            $"Failed to move '{fileName}' via drag-drop: {ex.Message}",
                            new Dictionary<string, string>
                            {
                                { "destination", destPath },
                                { "error", ex. Message }
                            }
                        );
                    }
                }
                completedOps = ops;
            });

            if (completedOps != null && completedOps.Any())
            {
                // Clear moved files from persistent selection
                foreach (var op in completedOps)
                {
                    PersistentSelectedPaths.Remove(op.ToPath);
                }

                _undoStack.Push(new UndoAction("Move", completedOps));
                UndoCommand.NotifyCanExecuteChanged();
                await UpdateAndSortLeftFilteredContentsAsync();
                await UpdateRightFilteredContentsAsync();
                SetOperationStatus($"Moved {completedOps.Count} file(s)");
            }
        }
        finally
        {
            IsOperationInProgress = false;
            UpdateButtonStates();
        }
    }
    #endregion
}