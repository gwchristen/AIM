using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class DirAnalysisViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly DirectoryOperationService _directoryOperationService;
    private readonly INavigationService _navigationService;
    private readonly IInfoBarService _infoBarService;

    #region Observable Properties
    [ObservableProperty]
    private string _analysisDirectory;

    [ObservableProperty]
    private ObservableCollection<OpCoStatItem> _opCoStats;

    [ObservableProperty]
    private ObservableCollection<FileAnomalyItem> _imInOhioAnomalies;

    [ObservableProperty]
    private ObservableCollection<FileAnomalyItem> _ohInImAnomalies;

    [ObservableProperty]
    private ObservableCollection<FileAnomalyItem> _unidentifiedAnomalies;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OhioDifference))]
    [NotifyPropertyChangedFor(nameof(OhioPercentage))]
    [NotifyPropertyChangedFor(nameof(OhioPercentageText))]
    private int _targetDevicesOhio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IMDifference))]
    [NotifyPropertyChangedFor(nameof(IMPercentage))]
    [NotifyPropertyChangedFor(nameof(IMPercentageText))]
    private int _targetDevicesIm;

    [ObservableProperty]
    private int _ohioFileCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OhioDifference))]
    [NotifyPropertyChangedFor(nameof(OhioPercentage))]
    [NotifyPropertyChangedFor(nameof(OhioPercentageText))]
    private int _ohioDeviceCount;

    [ObservableProperty]
    private int _imFileCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IMDifference))]
    [NotifyPropertyChangedFor(nameof(IMPercentage))]
    [NotifyPropertyChangedFor(nameof(IMPercentageText))]
    private int _imDeviceCount;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private string _analyzingText = "Analyzing directory... ";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _hasAnalyzed;

    [ObservableProperty]
    private string _lastAnalyzedText;

    [ObservableProperty]
    private bool _showResults;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImInOhioEmpty))]
    [NotifyPropertyChangedFor(nameof(OhInImEmpty))]
    [NotifyPropertyChangedFor(nameof(UnidentifiedEmpty))]
    [NotifyPropertyChangedFor(nameof(TotalAnomalyCount))]
    [NotifyPropertyChangedFor(nameof(TotalAnomalyBadgeColor))]
    [NotifyPropertyChangedFor(nameof(HasAnomalies))]
    private int _anomalyUpdateTrigger;

    [ObservableProperty]
    private IEnumerable<ISeries> _ohioGaugeSeries;

    [ObservableProperty]
    private IEnumerable<ISeries> _imGaugeSeries;
    #endregion

    #region Computed Properties
    public int OhioDifference => TargetDevicesOhio - OhioDeviceCount;
    public double OhioPercentage => TargetDevicesOhio > 0 ? (double)OhioDeviceCount / TargetDevicesOhio * 100 : 0;
    public string OhioPercentageText => $"{OhioPercentage:F1}%";

    public int IMDifference => TargetDevicesIm - ImDeviceCount;
    public double IMPercentage => TargetDevicesIm > 0 ? (double)ImDeviceCount / TargetDevicesIm * 100 : 0;
    public string IMPercentageText => $"{IMPercentage:F1}%";

    public bool CanRefresh => !string.IsNullOrEmpty(AnalysisDirectory) && !IsAnalyzing;

    public bool ImInOhioEmpty => ImInOhioAnomalies.Count == 0;
    public bool OhInImEmpty => OhInImAnomalies.Count == 0;
    public bool UnidentifiedEmpty => UnidentifiedAnomalies.Count == 0;

    public int TotalAnomalyCount => ImInOhioAnomalies.Count + OhInImAnomalies.Count + UnidentifiedAnomalies.Count;
    public bool HasAnomalies => TotalAnomalyCount > 0;

    public SolidColorBrush TotalAnomalyBadgeColor => TotalAnomalyCount == 0
        ? new SolidColorBrush(Microsoft.UI.Colors.Green)
        : new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
    #endregion

    public DirAnalysisViewModel(
        IDialogService dialogService,
        DirectoryOperationService directoryOperationService,
        INavigationService navigationService,
        IInfoBarService infoBarService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;
        _navigationService = navigationService;
        _infoBarService = infoBarService;

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

    partial void OnAnalysisDirectoryChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            RunAnalysisCommand.Execute(null);
        }
        else
        {
            ClearResults();
        }
        OnPropertyChanged(nameof(CanRefresh));
    }

    [RelayCommand]
    private async Task SelectAnalysisDirectoryAsync()
    {
        var path = await PickFolderAsync();
        if (path != null)
        {
            AnalysisDirectory = path;
        }
    }

    [RelayCommand]
    private async Task RunAnalysisAsync()
    {
        if (string.IsNullOrEmpty(AnalysisDirectory)) return;

        IsAnalyzing = true;
        HasError = false;
        ShowResults = false;
        AnalyzingText = "Scanning directories...";
        OnPropertyChanged(nameof(CanRefresh));

        ClearResults();

        try
        {
            AnalyzingText = "Counting files and devices...";
            var statsTask = _directoryOperationService.GetDirectoryStatsAsync(AnalysisDirectory);
            var newStatsTask = AnalyzeNewStatisticsAsync(AnalysisDirectory);

            await Task.WhenAll(statsTask, newStatsTask);

            var stats = await statsTask;
            foreach (var stat in stats)
            {
                OpCoStats.Add(stat);
            }

            AnalyzingText = "Checking for anomalies... ";
            await CheckFileAnomaliesAsync(AnalysisDirectory);

            // Trigger UI update for anomaly counts
            AnomalyUpdateTrigger++;

            HasAnalyzed = true;
            LastAnalyzedText = $"Last analyzed: {DateTime.Now:h:mm tt}";
            ShowResults = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsAnalyzing = false;
            OnPropertyChanged(nameof(CanRefresh));
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

        AnomalyUpdateTrigger++;
    }

    private async Task AnalyzeNewStatisticsAsync(string path)
    {
        var ohioPath = Path.Combine(path, "Ohio");
        var imPath = Path.Combine(path, "I&M");

        if (Directory.Exists(ohioPath))
        {
            var ohioFiles = await Task.Run(() => Directory.GetFiles(ohioPath, "*.*", SearchOption.AllDirectories));
            OhioFileCount = ohioFiles.Length;
            OhioDeviceCount = await CountLinesInFilesAsync(ohioFiles);
        }

        if (Directory.Exists(imPath))
        {
            var imFiles = await Task.Run(() => Directory.GetFiles(imPath, "*.*", SearchOption.AllDirectories));
            ImFileCount = imFiles.Length;
            ImDeviceCount = await CountLinesInFilesAsync(imFiles);
        }
    }

    private async Task CheckFileAnomaliesAsync(string path)
    {
        var imInOhioResults = new List<FileAnomalyItem>();
        var ohInImResults = new List<FileAnomalyItem>();
        var unidentifiedResults = new List<FileAnomalyItem>();

        await Task.Run(() =>
        {
            var ohioPath = Path.Combine(path, "Ohio");
            var imPath = Path.Combine(path, "I&M");

            var imTerms = new[] { "I&M", "I+M", "IM" };
            var ohTerms = new[] { "OH", "OP" };

            if (Directory.Exists(ohioPath))
            {
                var ohioFiles = Directory.GetFiles(ohioPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in ohioFiles)
                {
                    var fileName = Path.GetFileName(file);
                    bool hasImTerm = imTerms.Any(term => HasExactMatch(fileName, term));

                    if (hasImTerm)
                    {
                        imInOhioResults.Add(new FileAnomalyItem
                        {
                            FileName = fileName,
                            FilePath = file,
                            AnomalyType = "I&M file in Ohio"
                        });
                    }
                }
            }

            if (Directory.Exists(imPath))
            {
                var imFiles = Directory.GetFiles(imPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in imFiles)
                {
                    var fileName = Path.GetFileName(file);
                    bool hasOhTerm = ohTerms.Any(term => HasExactMatch(fileName, term));

                    if (hasOhTerm)
                    {
                        ohInImResults.Add(new FileAnomalyItem
                        {
                            FileName = fileName,
                            FilePath = file,
                            AnomalyType = "OH/OP file in I&M"
                        });
                    }
                }
            }

            if (Directory.Exists(ohioPath) && Directory.Exists(imPath))
            {
                var allOhioFiles = Directory.GetFiles(ohioPath, "*.*", SearchOption.AllDirectories);
                var allImFiles = Directory.GetFiles(imPath, "*.*", SearchOption.AllDirectories);
                var allFiles = allOhioFiles.Concat(allImFiles).ToList();

                foreach (var file in allFiles)
                {
                    var fileName = Path.GetFileName(file);
                    bool hasImTerm = imTerms.Any(term => HasExactMatch(fileName, term));
                    bool hasOhTerm = ohTerms.Any(term => HasExactMatch(fileName, term));

                    if (!hasImTerm && !hasOhTerm)
                    {
                        unidentifiedResults.Add(new FileAnomalyItem
                        {
                            FileName = fileName,
                            FilePath = file,
                            AnomalyType = "Unidentified"
                        });
                    }
                }
            }
        });

        foreach (var item in imInOhioResults)
            ImInOhioAnomalies.Add(item);

        foreach (var item in ohInImResults)
            OhInImAnomalies.Add(item);

        foreach (var item in unidentifiedResults)
            UnidentifiedAnomalies.Add(item);
    }

    private bool HasExactMatch(string fileName, string term)
    {
        var index = fileName.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return false;

        bool startOk = (index == 0 || !char.IsLetterOrDigit(fileName[index - 1]));
        bool endOk = (index + term.Length >= fileName.Length || !char.IsLetterOrDigit(fileName[index + term.Length]));

        return startOk && endOk;
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
            catch
            {
                // Skip files that can't be read
            }
        }
        return totalLines;
    }

    [RelayCommand]
    private async Task OpenFileLocationAsync(FileAnomalyItem item)
    {
        if (item == null) return;

        try
        {
            var folderPath = Path.GetDirectoryName(item.FilePath);
            await Windows.System.Launcher.LaunchFolderPathAsync(folderPath);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Error", $"Could not open folder: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void PreviewFile(FileAnomalyItem item)
    {
        if (item == null) return;

        var fileItem = new FileItem
        {
            Name = item.FileName,
            FullPath = item.FilePath,
            Type = Path.GetExtension(item.FilePath).ToLower() == ".csv" ? FileType.Csv : FileType.Text
        };

        _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
    }

    [RelayCommand]
    private void CopyPath(FileAnomalyItem item)
    {
        if (item == null) return;

        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(item.FilePath);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        _infoBarService.Show("Copied", "Path copied to clipboard.", InfoBarSeverity.Success, 2000);
    }

    [RelayCommand]
    private async Task ExportAnomaliesAsync()
    {
        if (!HasAnomalies)
        {
            _infoBarService.Show("No Anomalies", "There are no anomalies to export.", InfoBarSeverity.Warning);
            return;
        }

        var savePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"Anomalies_{DateTime.Now:yyyyMMdd_HHmmss}"
        };
        savePicker.FileTypeChoices.Add("Text File", new List<string> { ". txt" });
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
                sb.AppendLine("Category,FileName,FilePath");
            }
            else
            {
                sb.AppendLine($"Directory Analysis Anomaly Report");
                sb.AppendLine($"Generated: {DateTime.Now:f}");
                sb.AppendLine($"Directory: {AnalysisDirectory}");
                sb.AppendLine(new string('=', 60));
                sb.AppendLine();
            }

            if (ImInOhioAnomalies.Count > 0)
            {
                if (!isCsv)
                {
                    sb.AppendLine($"I&M Files in Ohio ({ImInOhioAnomalies.Count}):");
                    sb.AppendLine(new string('-', 40));
                }
                foreach (var item in ImInOhioAnomalies)
                {
                    if (isCsv)
                        sb.AppendLine($"\"I&M in Ohio\",\"{item.FileName}\",\"{item.FilePath}\"");
                    else
                        sb.AppendLine($"  {item.FileName}\n    {item.FilePath}");
                }
                if (!isCsv) sb.AppendLine();
            }

            if (OhInImAnomalies.Count > 0)
            {
                if (!isCsv)
                {
                    sb.AppendLine($"OH/OP Files in I&M ({OhInImAnomalies.Count}):");
                    sb.AppendLine(new string('-', 40));
                }
                foreach (var item in OhInImAnomalies)
                {
                    if (isCsv)
                        sb.AppendLine($"\"OH/OP in I&M\",\"{item.FileName}\",\"{item.FilePath}\"");
                    else
                        sb.AppendLine($"  {item.FileName}\n    {item.FilePath}");
                }
                if (!isCsv) sb.AppendLine();
            }

            if (UnidentifiedAnomalies.Count > 0)
            {
                if (!isCsv)
                {
                    sb.AppendLine($"Unidentified Files ({UnidentifiedAnomalies.Count}):");
                    sb.AppendLine(new string('-', 40));
                }
                foreach (var item in UnidentifiedAnomalies)
                {
                    if (isCsv)
                        sb.AppendLine($"\"Unidentified\",\"{item.FileName}\",\"{item.FilePath}\"");
                    else
                        sb.AppendLine($"  {item.FileName}\n    {item.FilePath}");
                }
            }

            await File.WriteAllTextAsync(file.Path, sb.ToString());
            _infoBarService.Show("Exported", $"Anomaly report saved to {file.Name}", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Export Failed", $"Could not save report: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    private async Task<string> PickFolderAsync()
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