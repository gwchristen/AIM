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
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class DirAnalysisViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;

    [ObservableProperty]
    private string? _analysisDirectory;

    [ObservableProperty]
    private ObservableCollection<OpCoStatItem> _opCoStats;

    [ObservableProperty]
    private ObservableCollection<string> _misplacedOhFiles;

    [ObservableProperty]
    private ObservableCollection<string> _misplacedImFiles;

    [ObservableProperty]
    private ObservableCollection<string> _unidentifiedFiles;

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
        _misplacedOhFiles = new ObservableCollection<string>();
        _misplacedImFiles = new ObservableCollection<string>();
        _unidentifiedFiles = new ObservableCollection<string>();
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

            // Run all analysis tasks
            var statsTask = _directoryOperationService.GetDirectoryStatsAsync(AnalysisDirectory);
            var anomalyTask = _directoryOperationService.FindFileAnomaliesAsync(AnalysisDirectory);
            var newStatsTask = AnalyzeNewStatisticsAsync(AnalysisDirectory);

            // Wait for all tasks to complete
            await Task.WhenAll(statsTask, anomalyTask, newStatsTask);

            // Now retrieve the results
            var stats = await statsTask;
            Debug.WriteLine($"[DirAnalysis] Retrieved {stats.Count} stat items");
            foreach (var stat in stats)
            {
                Debug.WriteLine($"[DirAnalysis] Adding stat: {stat.OpCoName} - {stat.FileCount} files, {stat.DeviceCount} devices");
                OpCoStats.Add(stat);
            }

            var report = await anomalyTask;
            Debug.WriteLine($"[DirAnalysis] Retrieved anomaly report - OH: {report.MisplacedOhFiles.Count}, IM: {report.MisplacedImFiles.Count}, Unidentified: {report.UnidentifiedFiles.Count}");

            report.MisplacedOhFiles.ForEach(MisplacedOhFiles.Add);
            report.MisplacedImFiles.ForEach(MisplacedImFiles.Add);
            report.UnidentifiedFiles.ForEach(UnidentifiedFiles.Add);

            // Log the Ohio and I&M counts
            Debug.WriteLine($"[DirAnalysis] Ohio - Files: {OhioFileCount}, Devices: {OhioDeviceCount}");
            Debug.WriteLine($"[DirAnalysis] I&M - Files: {ImFileCount}, Devices: {ImDeviceCount}");
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
        MisplacedOhFiles.Clear();
        MisplacedImFiles.Clear();
        UnidentifiedFiles.Clear();

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

        Debug.WriteLine($"[DirAnalysis] Looking for Ohio path: {ohioPath}");
        Debug.WriteLine($"[DirAnalysis] Looking for I&M path: {imPath}");

        if (Directory.Exists(ohioPath))
        {
            Debug.WriteLine($"[DirAnalysis] Ohio directory found");
            // Use SearchOption.AllDirectories to search recursively
            var ohioFiles = Directory.GetFiles(ohioPath, "*.*", System.IO.SearchOption.AllDirectories);
            OhioFileCount = ohioFiles.Length;
            Debug.WriteLine($"[DirAnalysis] Ohio files found: {OhioFileCount}");
            OhioDeviceCount = await CountLinesInFilesAsync(ohioFiles);
            Debug.WriteLine($"[DirAnalysis] Ohio device count: {OhioDeviceCount}");
        }
        else
        {
            Debug.WriteLine($"[DirAnalysis] Ohio directory NOT found at {ohioPath}");
        }

        if (Directory.Exists(imPath))
        {
            Debug.WriteLine($"[DirAnalysis] I&M directory found");
            // Use SearchOption.AllDirectories to search recursively
            var imFiles = Directory.GetFiles(imPath, "*.*", System.IO.SearchOption.AllDirectories);
            ImFileCount = imFiles.Length;
            Debug.WriteLine($"[DirAnalysis] I&M files found: {ImFileCount}");
            ImDeviceCount = await CountLinesInFilesAsync(imFiles);
            Debug.WriteLine($"[DirAnalysis] I&M device count: {ImDeviceCount}");
        }
        else
        {
            Debug.WriteLine($"[DirAnalysis] I&M directory NOT found at {imPath}");
        }

        Debug.WriteLine($"[DirAnalysis] AnalyzeNewStatisticsAsync completed");
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
                Debug.WriteLine($"[DirAnalysis] File {Path.GetFileName(file)}: {lines.Length} lines");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DirAnalysis] Error reading file {file}: {ex.Message}");
            }
        }
        Debug.WriteLine($"[DirAnalysis] Total lines in batch: {totalLines}");
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
}