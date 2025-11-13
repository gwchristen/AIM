using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace AIM.ViewModels;

public class LegendItem
{
    public string Name { get; set; }
    public long Count { get; set; }
    public double Percentage { get; set; }
    public SolidColorBrush ColorBrush { get; set; }
}

public partial class StatsViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;

    // THE FIX: Updated the colors as requested. Ohio is first, I&M is second.
    private readonly List<SKColor> _chartColors = new()
    {
        new SKColor(248, 113, 113), // Light Red
        new SKColor(96, 165, 250)   // Light Blue
    };

    public long TotalFileCount { get; private set; }
    public long TotalDeviceCount { get; private set; }
    public long ProblematicFileCount { get; private set; }
    public ISeries[] OpCoFileSeries { get; private set; } = new ISeries[0];
    public ISeries[] OpCoDeviceSeries { get; private set; } = new ISeries[0];
    public ObservableCollection<ProblematicFile> ProblematicFiles { get; } = new();
    public ObservableCollection<LegendItem> FileLegendItems { get; } = new();
    public ObservableCollection<LegendItem> DeviceLegendItems { get; } = new();

    public StatsViewModel(INavigationService navigationService, ISettingsService settingsService)
    {
        _navigationService = navigationService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    private async Task LoadStatsAsync()
    {
        await Task.Run(() =>
        {
            var appSettings = _settingsService.LoadSettings();
            var rootPath = appSettings.DefaultRootDirectory;
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return;

            var fileData = new[]
            {
                ("Ohio", (long)Directory.EnumerateFiles(Path.Combine(rootPath, "Ohio"), "*.txt", SearchOption.AllDirectories).Count()),
                ("I&M", (long)Directory.EnumerateFiles(Path.Combine(rootPath, "I&M"), "*.txt", SearchOption.AllDirectories).Count())
            };
            long totalFiles = fileData.Sum(x => x.Item2);

            long ohioLineCount = 0;
            long imLineCount = 0;
            var probFiles = new List<ProblematicFile>();
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.txt", SearchOption.AllDirectories))
            {
                try
                {
                    var lines = File.ReadAllLines(file);
                    var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    if (nonEmptyLines.Any(l => l.Length != 17)) { probFiles.Add(new ProblematicFile { Path = file }); }
                    if (file.StartsWith(Path.Combine(rootPath, "Ohio"), System.StringComparison.OrdinalIgnoreCase)) ohioLineCount += nonEmptyLines.Count;
                    if (file.StartsWith(Path.Combine(rootPath, "I&M"), System.StringComparison.OrdinalIgnoreCase)) imLineCount += nonEmptyLines.Count;
                }
                catch (IOException) { /* Skip */ }
            }
            var deviceData = new[] { ("Ohio", ohioLineCount), ("I&M", imLineCount) };
            long totalDevices = deviceData.Sum(x => x.Item2);

            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                TotalFileCount = totalFiles; OnPropertyChanged(nameof(TotalFileCount));
                TotalDeviceCount = totalDevices; OnPropertyChanged(nameof(TotalDeviceCount));
                ProblematicFileCount = probFiles.Count; OnPropertyChanged(nameof(ProblematicFileCount));
                ProblematicFiles.Clear();
                foreach (var pf in probFiles) ProblematicFiles.Add(pf);

                var fileSeries = new List<ISeries>();
                FileLegendItems.Clear();
                for (int i = 0; i < fileData.Length; i++)
                {
                    var (name, count) = fileData[i];
                    var color = _chartColors[i % _chartColors.Count];
                    fileSeries.Add(new PieSeries<long> { Name = name, Values = new[] { count }, Fill = new SolidColorPaint(color) });
                    FileLegendItems.Add(new LegendItem
                    {
                        Name = name,
                        Count = count,
                        Percentage = totalFiles == 0 ? 0 : (double)count / totalFiles,
                        ColorBrush = new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue))
                    });
                }
                OpCoFileSeries = fileSeries.ToArray();
                OnPropertyChanged(nameof(OpCoFileSeries));

                var deviceSeries = new List<ISeries>();
                DeviceLegendItems.Clear();
                for (int i = 0; i < deviceData.Length; i++)
                {
                    var (name, count) = deviceData[i];
                    var color = _chartColors[i % _chartColors.Count];
                    deviceSeries.Add(new PieSeries<long> { Name = name, Values = new[] { count }, Fill = new SolidColorPaint(color) });
                    DeviceLegendItems.Add(new LegendItem
                    {
                        Name = name,
                        Count = count,
                        Percentage = totalDevices == 0 ? 0 : (double)count / totalDevices,
                        ColorBrush = new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue))
                    });
                }
                OpCoDeviceSeries = deviceSeries.ToArray();
                OnPropertyChanged(nameof(OpCoDeviceSeries));
            });
        });
    }

    [RelayCommand]
    public void OpenFile(ProblematicFile file)
    {
        if (file == null) return;
        var fileItem = new FileItem { FullPath = file.Path, Name = Path.GetFileName(file.Path), Type = GetFileType(file.Path) };
        _navigationService.NavigateTo(typeof(Views.PreviewPage), fileItem);
    }
    private FileType GetFileType(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext switch { ".txt" => FileType.Text, ".csv" => FileType.Csv, _ => FileType.Other, };
    }
}