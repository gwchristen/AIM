using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class BatchRenamerViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;
    private readonly IInfoBarService _infoBarService;

    private static readonly string TxtExtension = ".txt";
    private static readonly string CsvExtension = ".csv";

    #region Observable Properties
    [ObservableProperty]
    private string _renameDirectory;

    [ObservableProperty]
    private bool _isRenaming;

    [ObservableProperty]
    private string _renameProgressText = "Renaming files...";

    [ObservableProperty]
    private bool _showResults;

    [ObservableProperty]
    private string _resultSummaryText;

    [ObservableProperty]
    private int _totalFileCount;

    [ObservableProperty]
    private int _subfolderCount;

    [ObservableProperty]
    private bool _hasDirectoryPreview;

    [ObservableProperty]
    private bool _canRename;
    #endregion

    public ObservableCollection<SubfolderPreview> PreviewSubfolders { get; } = new();
    public ObservableCollection<RenameResult> RenameResults { get; } = new();

    public BatchRenamerViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService, IInfoBarService infoBarService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
        _infoBarService = infoBarService;
    }

    private bool IsValidFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext.Equals(TxtExtension, StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(CsvExtension, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateState()
    {
        HasDirectoryPreview = !string.IsNullOrEmpty(RenameDirectory) && PreviewSubfolders.Count > 0;
        CanRename = !string.IsNullOrEmpty(RenameDirectory) && !IsRenaming && PreviewSubfolders.Count > 0;
        Debug.WriteLine($"[BatchRenamer] UpdateState: HasDirectoryPreview={HasDirectoryPreview}, CanRename={CanRename}, PreviewCount={PreviewSubfolders.Count}");
    }

    [RelayCommand]
    private async Task SelectRenameDirectoryAsync()
    {
        Debug.WriteLine("[BatchRenamer] SelectRenameDirectoryAsync called");

        var path = await PickFolderAsync();
        if (path != null)
        {
            Debug.WriteLine($"[BatchRenamer] Selected path: {path}");
            RenameDirectory = path;
            await LoadDirectoryPreviewAsync();
        }
        else
        {
            Debug.WriteLine("[BatchRenamer] No path selected");
        }
    }

    private async Task LoadDirectoryPreviewAsync()
    {
        Debug.WriteLine($"[BatchRenamer] LoadDirectoryPreviewAsync: {RenameDirectory}");

        PreviewSubfolders.Clear();
        TotalFileCount = 0;
        SubfolderCount = 0;

        if (string.IsNullOrEmpty(RenameDirectory) || !Directory.Exists(RenameDirectory))
        {
            Debug.WriteLine("[BatchRenamer] Directory empty or doesn't exist");
            UpdateState();
            return;
        }

        try
        {
            var subfolders = await Task.Run(() =>
            {
                var result = new List<SubfolderPreview>();
                ScanDirectoryRecursive(RenameDirectory, result, RenameDirectory);
                Debug.WriteLine($"[BatchRenamer] Found {result.Count} folders with files");
                return result.OrderBy(s => s.RelativePath).ToList();
            });

            Debug.WriteLine($"[BatchRenamer] Adding {subfolders.Count} subfolders to collection");

            // Calculate cumulative start numbers for preview
            int runningTotal = 1;
            foreach (var subfolder in subfolders)
            {
                subfolder.StartNumber = runningTotal;
                subfolder.EndNumber = runningTotal + subfolder.FileCount - 1;
                runningTotal += subfolder.FileCount;

                Debug.WriteLine($"[BatchRenamer] Adding: {subfolder.RelativePath} ({subfolder.FileCount} files, {subfolder.StartNumber}-{subfolder.EndNumber})");
                PreviewSubfolders.Add(subfolder);
            }

            TotalFileCount = PreviewSubfolders.Sum(s => s.FileCount);
            SubfolderCount = PreviewSubfolders.Count;

            Debug.WriteLine($"[BatchRenamer] Total: {TotalFileCount} files in {SubfolderCount} folders");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BatchRenamer] Error: {ex.Message}");
            _infoBarService.Show("Error", $"Could not load directory: {ex.Message}", InfoBarSeverity.Error);
        }

        UpdateState();
    }

    private void ScanDirectoryRecursive(string directory, List<SubfolderPreview> result, string rootPath)
    {
        try
        {
            var files = Directory.GetFiles(directory).Where(IsValidFile).ToList();

            Debug.WriteLine($"[BatchRenamer] Scanning: {directory} - Found {files.Count} matching files");

            if (files.Count > 0)
            {
                var relativePath = Path.GetRelativePath(rootPath, directory);
                result.Add(new SubfolderPreview
                {
                    Name = Path.GetFileName(directory),
                    FullPath = directory,
                    RelativePath = relativePath == "." ? "(root)" : relativePath,
                    FileCount = files.Count
                });
            }

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                ScanDirectoryRecursive(subDir, result, rootPath);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine($"[BatchRenamer] Access denied: {directory}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BatchRenamer] Error scanning {directory}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RenameFilesAsync()
    {
        Debug.WriteLine($"[BatchRenamer] RenameFilesAsync called.  TotalFileCount={TotalFileCount}");

        if (TotalFileCount == 0)
        {
            _infoBarService.Show("No Files", "No files found to rename in the selected directory.", InfoBarSeverity.Warning);
            return;
        }

        bool confirmed = await _dialogService.ShowConfirmationDialogAsync(
            "Confirm Batch Rename",
            $"Are you sure you want to rename {TotalFileCount} files in {SubfolderCount} folders?\n\n" +
            $"Files will be renamed sequentially (1, 2, 3, etc.) across ALL folders.\n\n" +
            "⚠️ This action is PERMANENT and cannot be undone!");

        if (!confirmed)
        {
            Debug.WriteLine("[BatchRenamer] User cancelled");
            return;
        }

        Debug.WriteLine("[BatchRenamer] Starting rename operation");

        IsRenaming = true;
        UpdateState();
        ShowResults = false;
        RenameResults.Clear();
        RenameProgressText = "Starting batch rename...";

        try
        {
            var foldersToProcess = PreviewSubfolders.Select(p => new
            {
                p.Name,
                p.FullPath,
                p.RelativePath
            }).ToList();

            var summary = new Dictionary<string, (int FilesRenamed, int StartNum, int EndNum)>();
            int processedFolders = 0;
            int totalFolders = foldersToProcess.Count;

            // Global counter that persists across all folders
            int globalCounter = 1;

            foreach (var subfolder in foldersToProcess)
            {
                processedFolders++;
                RenameProgressText = $"Processing ({processedFolders}/{totalFolders}): {subfolder.Name}... ";
                Debug.WriteLine($"[BatchRenamer] Processing: {subfolder.FullPath}, starting at {globalCounter}");

                int startNum = globalCounter;

                var filesRenamed = await Task.Run(() =>
                {
                    var files = Directory.GetFiles(subfolder.FullPath)
                        .Where(IsValidFile)
                        .OrderBy(f => f)
                        .ToList();

                    // First pass: rename to temp files to avoid conflicts
                    var tempMappings = new List<(string TempPath, string FinalPath)>();

                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file);
                        var tempPath = Path.Combine(subfolder.FullPath, $"_temp_{Guid.NewGuid()}{ext}");
                        var finalName = $"{globalCounter}{ext}";
                        var finalPath = Path.Combine(subfolder.FullPath, finalName);

                        File.Move(file, tempPath);
                        tempMappings.Add((tempPath, finalPath));

                        globalCounter++;
                    }

                    // Second pass: rename from temp to final names
                    foreach (var mapping in tempMappings)
                    {
                        File.Move(mapping.TempPath, mapping.FinalPath);
                    }

                    return files.Count;
                });

                int endNum = globalCounter - 1;
                Debug.WriteLine($"[BatchRenamer] Renamed {filesRenamed} files in {subfolder.Name} ({startNum}-{endNum})");
                summary[subfolder.RelativePath] = (filesRenamed, startNum, endNum);
            }

            int totalRenamed = 0;
            foreach (var entry in summary.OrderBy(e => e.Value.StartNum))
            {
                RenameResults.Add(new RenameResult
                {
                    FolderName = entry.Key,
                    FilesRenamed = entry.Value.FilesRenamed,
                    StartNumber = entry.Value.StartNum,
                    EndNumber = entry.Value.EndNum
                });
                totalRenamed += entry.Value.FilesRenamed;
            }

            ResultSummaryText = $"Successfully renamed {totalRenamed} files (1-{totalRenamed}) across {summary.Count} folders";
            ShowResults = true;

            Debug.WriteLine($"[BatchRenamer] Complete.Renamed {totalRenamed} files");
            _infoBarService.Show("Complete", $"Renamed {totalRenamed} files successfully.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BatchRenamer] Error: {ex.Message}");
            await _dialogService.ShowErrorDialogAsync("Rename Failed", $"An error occurred during renaming: {ex.Message}");
        }
        finally
        {
            IsRenaming = false;
            UpdateState();
        }
    }

    [RelayCommand]
    private async Task OpenDirectoryAsync()
    {
        if (!string.IsNullOrEmpty(RenameDirectory) && Directory.Exists(RenameDirectory))
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(RenameDirectory);
        }
    }

    [RelayCommand]
    private void ClearResults()
    {
        ShowResults = false;
        RenameResults.Clear();
        RenameDirectory = null;
        PreviewSubfolders.Clear();
        TotalFileCount = 0;
        SubfolderCount = 0;
        UpdateState();
    }

    private async Task<string> PickFolderAsync()
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            FileTypeFilter = { "*" }
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }
}

public class SubfolderPreview
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int StartNumber { get; set; }
    public int EndNumber { get; set; }
    public string FileCountText => $"{FileCount} files ({StartNumber}-{EndNumber})";
}

public class RenameResult
{
    public string FolderName { get; set; } = string.Empty;
    public int FilesRenamed { get; set; }
    public int StartNumber { get; set; }
    public int EndNumber { get; set; }
    public string RangeText => $"{StartNumber}-{EndNumber}";
}