using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI;

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
    [NotifyPropertyChangedFor(nameof(Step2Color))]
    [NotifyPropertyChangedFor(nameof(Step3Color))]
    private string _renameDirectory;

    [ObservableProperty]
    private string _baseFileName = string.Empty;

    [ObservableProperty]
    private int _startNumber = 1;

    [ObservableProperty]
    private int _paddingIndex = 0;

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

    [ObservableProperty]
    private bool _hasPreviewItems;

    [ObservableProperty]
    private int _previewItemCount;
    #endregion

    public ObservableCollection<SubfolderPreview> PreviewSubfolders { get; } = new();
    public ObservableCollection<RenameResult> RenameResults { get; } = new();
    public ObservableCollection<RenamePreviewItem> PreviewItems { get; } = new();

    private List<FilePreviewInfo> _allFiles = new();

    #region Computed Properties
    public SolidColorBrush Step2Color => !string.IsNullOrEmpty(RenameDirectory)
        ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 212))
        : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));

    public SolidColorBrush Step3Color => CanRename
        ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 212))
        : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
    #endregion

    public BatchRenamerViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService, IInfoBarService infoBarService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
        _infoBarService = infoBarService;
    }

    private string GenerateFileName(int number, string extension)
    {
        string formattedNumber = PaddingIndex switch
        {
            1 => number.ToString("D2"),
            2 => number.ToString("D3"),
            3 => number.ToString("D4"),
            _ => number.ToString()
        };

        if (string.IsNullOrEmpty(BaseFileName))
        {
            return $"{formattedNumber}{extension}";
        }
        else
        {
            return $"{BaseFileName}{formattedNumber}{extension}";
        }
    }

    partial void OnBaseFileNameChanged(string value) => UpdatePreview();
    partial void OnStartNumberChanged(int value) => UpdatePreview();
    partial void OnPaddingIndexChanged(int value) => UpdatePreview();

    private bool IsValidFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext.Equals(TxtExtension, StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(CsvExtension, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateState()
    {
        HasDirectoryPreview = !string.IsNullOrEmpty(RenameDirectory) && PreviewSubfolders.Count > 0;
        CanRename = !string.IsNullOrEmpty(RenameDirectory) && !IsRenaming && PreviewSubfolders.Count > 0 && TotalFileCount > 0;
    }

    private void UpdatePreview()
    {
        PreviewItems.Clear();

        if (_allFiles.Count == 0)
        {
            HasPreviewItems = false;
            PreviewItemCount = 0;
            return;
        }

        int currentNumber = StartNumber;

        foreach (var file in _allFiles)
        {
            var newName = GenerateFileName(currentNumber, file.Extension);
            PreviewItems.Add(new RenamePreviewItem
            {
                OldName = file.FileName,
                NewName = newName,
                FolderPath = file.RelativePath
            });
            currentNumber++;
        }

        HasPreviewItems = PreviewItems.Count > 0;
        PreviewItemCount = _allFiles.Count;
    }

    [RelayCommand]
    private async Task SelectRenameDirectoryAsync()
    {
        var path = await PickFolderAsync();
        if (path != null)
        {
            RenameDirectory = path;
            await LoadDirectoryPreviewAsync();
        }
    }

    private async Task LoadDirectoryPreviewAsync()
    {
        PreviewSubfolders.Clear();
        PreviewItems.Clear();
        _allFiles.Clear();
        TotalFileCount = 0;
        SubfolderCount = 0;

        if (string.IsNullOrEmpty(RenameDirectory) || !Directory.Exists(RenameDirectory))
        {
            UpdateState();
            UpdatePreview();
            return;
        }

        try
        {
            var (subfolders, allFiles) = await Task.Run(() =>
            {
                var folderResult = new List<SubfolderPreview>();
                var fileResult = new List<FilePreviewInfo>();
                ScanDirectoryRecursive(RenameDirectory, folderResult, fileResult, RenameDirectory);
                return (folderResult.OrderBy(s => s.RelativePath).ToList(), fileResult);
            });

            _allFiles = allFiles;

            int runningTotal = StartNumber;
            foreach (var subfolder in subfolders)
            {
                subfolder.StartNumber = runningTotal;
                subfolder.EndNumber = runningTotal + subfolder.FileCount - 1;
                runningTotal += subfolder.FileCount;
                PreviewSubfolders.Add(subfolder);
            }

            TotalFileCount = _allFiles.Count;
            SubfolderCount = PreviewSubfolders.Count;

            UpdatePreview();
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not load directory: {ex.Message}", InfoBarSeverity.Error);
        }

        UpdateState();
    }

    private void ScanDirectoryRecursive(string directory, List<SubfolderPreview> folderResult, List<FilePreviewInfo> fileResult, string rootPath)
    {
        try
        {
            var files = Directory.GetFiles(directory)
                .Where(IsValidFile)
                .OrderBy(f => f)
                .ToList();

            if (files.Count > 0)
            {
                var relativePath = Path.GetRelativePath(rootPath, directory);
                relativePath = relativePath == "." ? "(root)" : relativePath;

                folderResult.Add(new SubfolderPreview
                {
                    Name = Path.GetFileName(directory),
                    FullPath = directory,
                    RelativePath = relativePath,
                    FileCount = files.Count
                });

                foreach (var file in files)
                {
                    fileResult.Add(new FilePreviewInfo
                    {
                        FullPath = file,
                        FileName = Path.GetFileName(file),
                        Extension = Path.GetExtension(file),
                        RelativePath = relativePath
                    });
                }
            }

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                ScanDirectoryRecursive(subDir, folderResult, fileResult, rootPath);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch { }
    }

    [RelayCommand]
    private void RefreshPreview()
    {
        UpdatePreview();
    }

    [RelayCommand]
    private async Task RenameFilesAsync()
    {
        if (TotalFileCount == 0)
        {
            _infoBarService.Show("No Files", "No files found to rename.", InfoBarSeverity.Warning);
            return;
        }

        string patternExample = GenerateFileName(StartNumber, ". txt") + ", " +
                               GenerateFileName(StartNumber + 1, ".txt") + ", ... ";

        bool confirmed = await _dialogService.ShowConfirmationDialogAsync(
            "Confirm Batch Rename",
            $"Are you sure you want to rename {TotalFileCount} files in {SubfolderCount} folders?\n\n" +
            $"Naming Pattern: {patternExample}\n\n" +
            "⚠️ This action is PERMANENT and cannot be undone!");

        if (!confirmed) return;

        IsRenaming = true;
        UpdateState();
        ShowResults = false;
        RenameResults.Clear();
        RenameProgressText = "Starting batch rename...";

        var stopwatch = Stopwatch.StartNew();

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
            int globalCounter = StartNumber;

            foreach (var subfolder in foldersToProcess)
            {
                processedFolders++;
                RenameProgressText = $"Processing ({processedFolders}/{totalFolders}): {subfolder.Name}... ";

                int startNum = globalCounter;

                var filesRenamed = await Task.Run(() =>
                {
                    var files = Directory.GetFiles(subfolder.FullPath)
                        .Where(IsValidFile)
                        .OrderBy(f => f)
                        .ToList();

                    var tempMappings = new List<(string TempPath, string FinalPath)>();

                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file);
                        var tempPath = Path.Combine(subfolder.FullPath, $"_temp_{Guid.NewGuid()}{ext}");
                        var finalName = GenerateFileName(globalCounter, ext);
                        var finalPath = Path.Combine(subfolder.FullPath, finalName);

                        File.Move(file, tempPath);
                        tempMappings.Add((tempPath, finalPath));
                        globalCounter++;
                    }

                    foreach (var mapping in tempMappings)
                    {
                        File.Move(mapping.TempPath, mapping.FinalPath);
                    }

                    return files.Count;
                });

                int endNum = globalCounter - 1;
                summary[subfolder.RelativePath] = (filesRenamed, startNum, endNum);
            }

            stopwatch.Stop();

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

            string elapsedTime = FormatElapsedTime(stopwatch.ElapsedMilliseconds);
            string exampleRange = $"{GenerateFileName(StartNumber, ".txt")} to {GenerateFileName(StartNumber + totalRenamed - 1, ".txt")}";
            ResultSummaryText = $"Renamed {totalRenamed} files ({exampleRange}) in {elapsedTime}";
            ShowResults = true;

            _infoBarService.Show("Rename Complete! ",
                $"Successfully renamed {totalRenamed} files in {elapsedTime}.",
                InfoBarSeverity.Success,
                5000);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Rename Failed", ex.Message, InfoBarSeverity.Error);
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
            try
            {
                await Windows.System.Launcher.LaunchFolderPathAsync(RenameDirectory);
            }
            catch (Exception ex)
            {
                _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
            }
        }
    }

    [RelayCommand]
    private void ClearResults()
    {
        ShowResults = false;
        RenameResults.Clear();
        RenameDirectory = null;
        PreviewSubfolders.Clear();
        PreviewItems.Clear();
        _allFiles.Clear();
        TotalFileCount = 0;
        SubfolderCount = 0;
        BaseFileName = string.Empty;
        StartNumber = 1;
        PaddingIndex = 0;
        UpdateState();
        UpdatePreview();
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

    private static string FormatElapsedTime(long milliseconds)
    {
        if (milliseconds < 1000) return $"{milliseconds}ms";
        if (milliseconds < 60000) return $"{milliseconds / 1000.0:F1}s";
        return $"{milliseconds / 60000.0:F1}m";
    }
}

#region Models

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

public class RenamePreviewItem
{
    public string OldName { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
}

public class FilePreviewInfo
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
}

#endregion