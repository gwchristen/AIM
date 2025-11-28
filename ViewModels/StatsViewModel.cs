using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI;

namespace AIM.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly IInfoBarService _infoBarService;

    private static readonly SolidColorBrush GreenBrush = new(Color.FromArgb(255, 16, 124, 16));
    private static readonly SolidColorBrush RedBrush = new(Color.FromArgb(255, 196, 43, 28));

    #region Observable Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFileCountText))]
    private int _totalFileCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalDeviceCountText))]
    private long _totalDeviceCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblematicFiles))]
    [NotifyPropertyChangedFor(nameof(HasNoProblematicFiles))]
    [NotifyPropertyChangedFor(nameof(ProblematicFileCountText))]
    [NotifyPropertyChangedFor(nameof(ProblematicBorderColor))]
    [NotifyPropertyChangedFor(nameof(ProblematicTextColor))]
    [NotifyPropertyChangedFor(nameof(ProblematicBadgeColor))]
    [NotifyPropertyChangedFor(nameof(ProblematicIcon))]
    private int _problematicFileCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingText = "Loading statistics...";

    [ObservableProperty]
    private bool _hasLoaded;

    [ObservableProperty]
    private string _lastRefreshedText;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _showNoDirectory;

    [ObservableProperty]
    private bool _showResults;

    [ObservableProperty]
    private string _sourceDirectoryText = "Configure root directory in Settings";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoChartData))]
    private int _chartDataCount;
    #endregion

    public ObservableCollection<ISeries> OpCoFileSeries { get; set; } = new();
    public ObservableCollection<ISeries> OpCoDeviceSeries { get; set; } = new();
    public ObservableCollection<ProblematicFile> ProblematicFiles { get; set; } = new();

    #region Computed Properties
    public string TotalFileCountText => TotalFileCount.ToString("N0");
    public string TotalDeviceCountText => TotalDeviceCount.ToString("N0");
    public string ProblematicFileCountText => ProblematicFileCount.ToString("N0");

    public bool HasProblematicFiles => ProblematicFileCount > 0;
    public bool HasNoProblematicFiles => ProblematicFileCount == 0 && HasLoaded;
    public bool HasNoChartData => ChartDataCount == 0 && HasLoaded;

    public SolidColorBrush ProblematicBorderColor => ProblematicFileCount == 0 ? GreenBrush : RedBrush;
    public SolidColorBrush ProblematicTextColor => ProblematicFileCount == 0 ? GreenBrush : RedBrush;
    public SolidColorBrush ProblematicBadgeColor => ProblematicFileCount == 0 ? GreenBrush : RedBrush;
    public string ProblematicIcon => ProblematicFileCount == 0 ? "\uE73E" : "\uE7BA";
    #endregion

    public StatsViewModel(ISettingsService settingsService, INavigationService navigationService, IInfoBarService infoBarService, IRefreshService refreshService)
    {
        _settingsService = settingsService;
        _navigationService = navigationService;
        _infoBarService = infoBarService;
        refreshService.RefreshRequested += (s, e) => LoadStatsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadStats()
    {
        IsLoading = true;
        HasError = false;
        ShowNoDirectory = false;
        ShowResults = false;
        LoadingText = "Checking configuration...";

        var settings = _settingsService.LoadSettings();
        var rootPath = settings.DefaultRootDirectory;

        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
        {
            IsLoading = false;
            ShowNoDirectory = true;
            SourceDirectoryText = "Configure root directory in Settings";
            return;
        }

        SourceDirectoryText = rootPath;

        OpCoFileSeries.Clear();
        OpCoDeviceSeries.Clear();
        ProblematicFiles.Clear();

        try
        {
            LoadingText = "Scanning directories...";

            var problematicList = new List<ProblematicFile>();
            var allStats = new List<OpCoStatItem>();
            int totalFiles = 0;
            long totalDevices = 0;

            await Task.Run(async () =>
            {
                var opCoDirs = Directory.GetDirectories(rootPath);

                foreach (var dirPath in opCoDirs)
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    var files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);

                    int fileCount = files.Length;
                    long deviceCount = 0;

                    // Count lines (devices) in each file
                    foreach (var file in files)
                    {
                        try
                        {
                            var lines = await File.ReadAllLinesAsync(file.FullName);
                            deviceCount += lines.Length;

                            // Check for problematic files (lines != 17 characters)
                            foreach (var line in lines)
                            {
                                if (line.Length != 17)
                                {
                                    problematicList.Add(new ProblematicFile
                                    {
                                        Path = file.FullName,
                                        FileName = file.Name,
                                        Directory = file.DirectoryName ?? "",
                                        Size = file.Length
                                    });
                                    break; // Only add file once
                                }
                            }
                        }
                        catch
                        {
                            // Skip files that can't be read
                        }
                    }

                    allStats.Add(new OpCoStatItem
                    {
                        OpCoName = dirInfo.Name,
                        FileCount = fileCount,
                        DeviceCount = deviceCount
                    });

                    totalFiles += fileCount;
                    totalDevices += deviceCount;
                }
            });

            // Update UI on main thread
            TotalFileCount = totalFiles;
            TotalDeviceCount = totalDevices;

            foreach (var file in problematicList.DistinctBy(f => f.Path))
            {
                ProblematicFiles.Add(file);
            }
            ProblematicFileCount = ProblematicFiles.Count;

            var fileSeries = allStats.Select(s => new PieSeries<int>
            {
                Name = s.OpCoName,
                Values = new int[] { s.FileCount },
                DataLabelsSize = 10
            });
            var deviceSeries = allStats.Select(s => new PieSeries<long>
            {
                Name = s.OpCoName,
                Values = new long[] { s.DeviceCount },
                DataLabelsSize = 10
            });

            foreach (var series in fileSeries) OpCoFileSeries.Add(series);
            foreach (var series in deviceSeries) OpCoDeviceSeries.Add(series);

            ChartDataCount = allStats.Count;
            HasLoaded = true;
            LastRefreshedText = $"Last refreshed: {DateTime.Now:h:mm tt}";
            ShowResults = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void OpenFile(ProblematicFile file)
    {
        if (file == null) return;
        var fileItem = new FileItem
        {
            Name = file.FileName,
            FullPath = file.Path,
            Type = Path.GetExtension(file.Path).ToLower() == ".csv" ? FileType.Csv : FileType.Text
        };
        _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
    }

    [RelayCommand]
    private async Task OpenFileLocationAsync(ProblematicFile file)
    {
        if (file == null) return;

        try
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(file.Directory);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void CopyPath(ProblematicFile file)
    {
        if (file == null) return;

        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(file.Path);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        _infoBarService.Show("Copied", "Path copied to clipboard.", InfoBarSeverity.Success, 2000);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _navigationService.NavigateTo(typeof(SettingsPage));
    }

    [RelayCommand]
    private async Task ExportProblematicFilesAsync()
    {
        if (!HasProblematicFiles)
        {
            _infoBarService.Show("No Files", "There are no problematic files to export.", InfoBarSeverity.Warning);
            return;
        }

        var savePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"ProblematicFiles_{DateTime.Now:yyyyMMdd_HHmmss}"
        };
        savePicker.FileTypeChoices.Add("Text File", new List<string> { ".txt" });
        savePicker.FileTypeChoices.Add("CSV File", new List<string> { ".csv" });

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

        var file = await savePicker.PickSaveFileAsync();
        if (file == null) return;

        try
        {
            var sb = new StringBuilder();
            var isCsv = file.FileType.ToLower() == ".csv";

            if (isCsv)
            {
                sb.AppendLine("FileName,Directory,Size,FullPath");
                foreach (var f in ProblematicFiles)
                {
                    sb.AppendLine($"\"{f.FileName}\",\"{f.Directory}\",{f.Size},\"{f.Path}\"");
                }
            }
            else
            {
                sb.AppendLine("Problematic Files Report");
                sb.AppendLine($"Generated: {DateTime.Now:f}");
                sb.AppendLine($"Total Files: {ProblematicFileCount}");
                sb.AppendLine(new string('=', 60));
                sb.AppendLine();

                foreach (var f in ProblematicFiles)
                {
                    sb.AppendLine($"{f.FileName}");
                    sb.AppendLine($"  Size: {f.SizeText}");
                    sb.AppendLine($"  Path: {f.Path}");
                    sb.AppendLine();
                }
            }

            await File.WriteAllTextAsync(file.Path, sb.ToString());
            _infoBarService.Show("Exported", $"Report saved to {file.Name}", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Export Failed", $"Could not save report: {ex.Message}", InfoBarSeverity.Error);
        }
    }
}