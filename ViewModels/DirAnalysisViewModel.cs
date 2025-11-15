using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;

namespace AIM.ViewModels;

public partial class DirAnalysisViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;

    [ObservableProperty]
    private string? _analysisDirectory;

    [ObservableProperty]
    private ObservableCollection<OpCoStatItem> _opCoStats;

    // File Anomaly Collections
    [ObservableProperty]
    private ObservableCollection<FileAnomalyItem> _imInOhioAnomalies;

    [ObservableProperty]
    private ObservableCollection<FileAnomalyItem> _ohInImAnomalies;

    [ObservableProperty]
    private ObservableCollection<FileAnomalyItem> _unidentifiedAnomalies;

    // Separate target device properties for Ohio and I&M
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OhioDifference))]
    [NotifyPropertyChangedFor(nameof(OhioPercentage))]
    private int _targetDevicesOhio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IMDifference))]
    [NotifyPropertyChangedFor(nameof(IMPercentage))]
    private int _targetDevicesIm;

    [ObservableProperty]
    private int _ohioFileCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OhioDifference))]
    [NotifyPropertyChangedFor(nameof(OhioPercentage))]
    private int _ohioDeviceCount;

    [ObservableProperty]
    private int _imFileCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IMDifference))]
    [NotifyPropertyChangedFor(nameof(IMPercentage))]
    private int _imDeviceCount;

    public int OhioDifference => TargetDevicesOhio - OhioDeviceCount;
    public double OhioPercentage => TargetDevicesOhio > 0 ? (double)OhioDeviceCount / TargetDevicesOhio * 100 : 0;

    public int IMDifference => TargetDevicesIm - ImDeviceCount;
    public double IMPercentage => TargetDevicesIm > 0 ? (double)ImDeviceCount / TargetDevicesIm * 100 : 0;

    // Gauge Series Properties
    [ObservableProperty]
    private IEnumerable<ISeries> _ohioGaugeSeries;

    [ObservableProperty]
    private IEnumerable<ISeries> _imGaugeSeries;

    public DirAnalysisViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;

        _opCoStats = new ObservableCollection<OpCoStatItem>();
        _imInOhioAnomalies = new ObservableCollection<FileAnomalyItem>();
        _ohInImAnomalies = new ObservableCollection<FileAnomalyItem>();
        _unidentifiedAnomalies = new ObservableCollection<FileAnomalyItem>();
        _ohioGaugeSeries = CreateGaugeSeries(0, 0);
        _imGaugeSeries = CreateGaugeSeries(0, 0);
    }

    partial void OnTargetDevicesOhioChanged(int value)
    {
        OhioGaugeSeries = CreateGaugeSeries(OhioDeviceCount, value);
    }

    partial void OnTargetDevicesImChanged(int value)
    {
        ImGaugeSeries = CreateGaugeSeries(ImDeviceCount, value);
    }

    partial void OnOhioDeviceCountChanged(int value)
    {
        OhioGaugeSeries = CreateGaugeSeries(value, TargetDevicesOhio);
    }

    partial void OnImDeviceCountChanged(int value)
    {
        ImGaugeSeries = CreateGaugeSeries(value, TargetDevicesIm);
    }

    private IEnumerable<ISeries> CreateGaugeSeries(int value, int target)
    {
        int remaining = Math.Max(0, target - value);

        return new ISeries[]
        {
            new PieSeries<int>
            {
                Values = new[] { value },
                InnerRadius = 50
            },
            new PieSeries<int>
            {
                Values = new[] { remaining },
                InnerRadius = 50,
                Fill = new SolidColorPaint(new SKColor(200, 200, 200, 100))
            }
        };
    }

    partial void OnAnalysisDirectoryChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            RunAnalysisCommand.Execute(null);
        }
        else
        {
            ClearResults();
        }
    }

    [RelayCommand]
    private async Task SelectAnalysisDirectoryAsync() => AnalysisDirectory = await PickFolderAsync();

    [RelayCommand]
    private async Task RunAnalysisAsync()
    {
        if (string.IsNullOrEmpty(AnalysisDirectory)) return;

        ClearResults();

        try
        {
            Debug.WriteLine($"[DirAnalysis] Starting analysis of: {AnalysisDirectory}");

            var statsTask = _directoryOperationService.GetDirectoryStatsAsync(AnalysisDirectory);
            var newStatsTask = AnalyzeNewStatisticsAsync(AnalysisDirectory);
            var anomalyTask = CheckFileAnomaliesAsync(AnalysisDirectory);

            await Task.WhenAll(statsTask, newStatsTask, anomalyTask);

            var stats = await statsTask;
            Debug.WriteLine($"[DirAnalysis] Retrieved {stats.Count} stat items");
            foreach (var stat in stats)
            {
                Debug.WriteLine($"[DirAnalysis] Adding stat: {stat.OpCoName} - {stat.FileCount} files, {stat.DeviceCount} devices");
                OpCoStats.Add(stat);
            }

            Debug.WriteLine($"[DirAnalysis] Ohio - Files: {OhioFileCount}, Devices: {OhioDeviceCount}");
            Debug.WriteLine($"[DirAnalysis] I&M - Files: {ImFileCount}, Devices: {ImDeviceCount}");
            Debug.WriteLine($"[DirAnalysis] I&M in Ohio: {ImInOhioAnomalies.Count}, OH in I&M: {OhInImAnomalies.Count}, Unidentified: {UnidentifiedAnomalies.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DirAnalysis] Error: {ex.Message}");
            Debug.WriteLine($"[DirAnalysis] StackTrace: {ex.StackTrace}");
            await _dialogService.ShowErrorDialogAsync("Analysis Failed", $"An error occurred.\nError: {ex.Message}");
        }
    }

    private void ClearResults()
    {
        OpCoStats.Clear();
        ImInOhioAnomalies.Clear();
        OhInImAnomalies.Clear();
        UnidentifiedAnomalies.Clear();

        OhioFileCount = 0;
        OhioDeviceCount = 0;
        ImFileCount = 0;
        ImDeviceCount = 0;

        OhioGaugeSeries = CreateGaugeSeries(0, TargetDevicesOhio);
        ImGaugeSeries = CreateGaugeSeries(0, TargetDevicesIm);
    }

    private async Task AnalyzeNewStatisticsAsync(string path)
    {
        Debug.WriteLine($"[DirAnalysis] AnalyzeNewStatisticsAsync called with path: {path}");

        var ohioPath = Path.Combine(path, "Ohio");
        var imPath = Path.Combine(path, "I&M");

        if (Directory.Exists(ohioPath))
        {
            Debug.WriteLine($"[DirAnalysis] Ohio directory found");
            var ohioFiles = Directory.GetFiles(ohioPath, "*.*", System.IO.SearchOption.AllDirectories);
            OhioFileCount = ohioFiles.Length;
            Debug.WriteLine($"[DirAnalysis] Ohio files found: {OhioFileCount}");
            OhioDeviceCount = await CountLinesInFilesAsync(ohioFiles);
            Debug.WriteLine($"[DirAnalysis] Ohio device count: {OhioDeviceCount}");
        }
        else
        {
            Debug.WriteLine($"[DirAnalysis] Ohio directory NOT found");
        }

        if (Directory.Exists(imPath))
        {
            Debug.WriteLine($"[DirAnalysis] I&M directory found");
            var imFiles = Directory.GetFiles(imPath, "*.*", System.IO.SearchOption.AllDirectories);
            ImFileCount = imFiles.Length;
            Debug.WriteLine($"[DirAnalysis] I&M files found: {ImFileCount}");
            ImDeviceCount = await CountLinesInFilesAsync(imFiles);
            Debug.WriteLine($"[DirAnalysis] I&M device count: {ImDeviceCount}");
        }
        else
        {
            Debug.WriteLine($"[DirAnalysis] I&M directory NOT found");
        }

        Debug.WriteLine($"[DirAnalysis] AnalyzeNewStatisticsAsync completed");
    }

    private async Task CheckFileAnomaliesAsync(string path)
    {
        // Collect results on background thread
        var imInOhioResults = new List<FileAnomalyItem>();
        var ohInImResults = new List<FileAnomalyItem>();
        var unidentifiedResults = new List<FileAnomalyItem>();

        await Task.Run(() =>
        {
            Debug.WriteLine($"[DirAnalysis] CheckFileAnomaliesAsync called");

            var ohioPath = Path.Combine(path, "Ohio");
            var imPath = Path.Combine(path, "I&M");

            // These terms must be exact (whole word or clear boundary)
            var imTerms = new[] { "I&M", "I+M", "IM" };
            var ohTerms = new[] { "OH", "OP" };

            // Check Ohio directory for I&M/I+M/IM files (these shouldn't be here)
            if (Directory.Exists(ohioPath))
            {
                Debug.WriteLine($"[DirAnalysis] Checking Ohio directory for misplaced I&M files");
                var ohioFiles = Directory.GetFiles(ohioPath, "*.*", System.IO.SearchOption.AllDirectories);

                foreach (var file in ohioFiles)
                {
                    var fileName = Path.GetFileName(file);
                    // Check for exact term match (case insensitive)
                    bool hasImTerm = imTerms.Any(term =>
                    {
                        var index = fileName.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                        if (index < 0) return false;

                        // For exact match, check word boundaries
                        bool startOk = (index == 0 || !char.IsLetterOrDigit(fileName[index - 1]));
                        bool endOk = (index + term.Length >= fileName.Length || !char.IsLetterOrDigit(fileName[index + term.Length]));

                        return startOk && endOk;
                    });

                    if (hasImTerm)
                    {
                        imInOhioResults.Add(new FileAnomalyItem
                        {
                            FileName = fileName,
                            FilePath = file,
                            AnomalyType = "I&M file in Ohio"
                        });
                        Debug.WriteLine($"[DirAnalysis] Found I&M in Ohio: {fileName}");
                    }
                }
            }

            // Check I&M directory for OH/OP files (these shouldn't be here)
            if (Directory.Exists(imPath))
            {
                Debug.WriteLine($"[DirAnalysis] Checking I&M directory for misplaced OH/OP files");
                var imFiles = Directory.GetFiles(imPath, "*.*", System.IO.SearchOption.AllDirectories);

                foreach (var file in imFiles)
                {
                    var fileName = Path.GetFileName(file);
                    // Check for exact term match (case insensitive)
                    bool hasOhTerm = ohTerms.Any(term =>
                    {
                        var index = fileName.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                        if (index < 0) return false;

                        // For exact match, check word boundaries
                        bool startOk = (index == 0 || !char.IsLetterOrDigit(fileName[index - 1]));
                        bool endOk = (index + term.Length >= fileName.Length || !char.IsLetterOrDigit(fileName[index + term.Length]));

                        return startOk && endOk;
                    });

                    if (hasOhTerm)
                    {
                        ohInImResults.Add(new FileAnomalyItem
                        {
                            FileName = fileName,
                            FilePath = file,
                            AnomalyType = "OH/OP file in I&M"
                        });
                        Debug.WriteLine($"[DirAnalysis] Found OH/OP in I&M: {fileName}");
                    }
                }
            }

            // Check for unidentified files (files with NO identifiers at all)
            if (Directory.Exists(ohioPath) && Directory.Exists(imPath))
            {
                Debug.WriteLine($"[DirAnalysis] Checking for unidentified files");
                var allOhioFiles = Directory.GetFiles(ohioPath, "*.*", System.IO.SearchOption.AllDirectories);
                var allImFiles = Directory.GetFiles(imPath, "*.*", System.IO.SearchOption.AllDirectories);
                var allFiles = allOhioFiles.Concat(allImFiles).ToList();

                foreach (var file in allFiles)
                {
                    var fileName = Path.GetFileName(file);

                    // Check if file has ANY identifying term (exact match)
                    bool hasImTerm = imTerms.Any(term =>
                    {
                        var index = fileName.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                        if (index < 0) return false;

                        bool startOk = (index == 0 || !char.IsLetterOrDigit(fileName[index - 1]));
                        bool endOk = (index + term.Length >= fileName.Length || !char.IsLetterOrDigit(fileName[index + term.Length]));

                        return startOk && endOk;
                    });

                    bool hasOhTerm = ohTerms.Any(term =>
                    {
                        var index = fileName.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                        if (index < 0) return false;

                        bool startOk = (index == 0 || !char.IsLetterOrDigit(fileName[index - 1]));
                        bool endOk = (index + term.Length >= fileName.Length || !char.IsLetterOrDigit(fileName[index + term.Length]));

                        return startOk && endOk;
                    });

                    // Only flag as unidentified if it has NEITHER term
                    if (!hasImTerm && !hasOhTerm)
                    {
                        unidentifiedResults.Add(new FileAnomalyItem
                        {
                            FileName = fileName,
                            FilePath = file,
                            AnomalyType = "Unidentified"
                        });
                        Debug.WriteLine($"[DirAnalysis] Found unidentified file: {fileName}");
                    }
                }
            }

            Debug.WriteLine($"[DirAnalysis] File anomaly check completed - I&M in Ohio: {imInOhioResults.Count}, OH/OP in I&M: {ohInImResults.Count}, Unidentified: {unidentifiedResults.Count}");
        });

        // Update collections on UI thread
        foreach (var item in imInOhioResults)
        {
            ImInOhioAnomalies.Add(item);
        }

        foreach (var item in ohInImResults)
        {
            OhInImAnomalies.Add(item);
        }

        foreach (var item in unidentifiedResults)
        {
            UnidentifiedAnomalies.Add(item);
        }

        Debug.WriteLine($"[DirAnalysis] Collections updated - I&M in Ohio: {ImInOhioAnomalies.Count}, OH/OP in I&M: {OhInImAnomalies.Count}, Unidentified: {UnidentifiedAnomalies.Count}");
    }

    private async Task<int> CountLinesInFilesAsync(string[] files)
    {
        int totalLines = 0;
        foreach (var file in files)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                totalLines += lines.Length;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DirAnalysis] Error reading file {file}: {ex.Message}");
            }
        }
        return totalLines;
    }

    private async Task<string?> PickFolderAsync()
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
        };
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    public string GetProgressBarColor(double percentage)
    {
        if (percentage <= 0) return "Red";
        if (percentage < 50) return "Orange"; // Red to Yellow
        if (percentage < 100) return "Yellow";
        return "Green"; // 100% or more
    }
}