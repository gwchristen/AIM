using AIM.Models;
using AIM.Services;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly IInfoBarService _infoBarService;

    [ObservableProperty]
    private int _totalFileCount;

    [ObservableProperty]
    private long _totalDeviceCount;

    [ObservableProperty]
    private int _problematicFileCount;

    public ObservableCollection<ISeries> OpCoFileSeries { get; set; } = new();
    public ObservableCollection<ISeries> OpCoDeviceSeries { get; set; } = new();
    public ObservableCollection<ProblematicFile> ProblematicFiles { get; set; } = new();

    public StatsViewModel(ISettingsService settingsService, INavigationService navigationService, IInfoBarService infoBarService)
    {
        _settingsService = settingsService;
        _navigationService = navigationService;
        _infoBarService = infoBarService;
    }

    [RelayCommand]
    private async Task LoadStats()
    {
        var settings = _settingsService.LoadSettings();
        var rootPath = settings.DefaultRootDirectory;
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
        {
            _infoBarService.Show("Error", "Root directory not set or not found. Please configure it in Settings.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            return;
        }

        OpCoFileSeries.Clear();
        OpCoDeviceSeries.Clear();
        ProblematicFiles.Clear();

        await Task.Run(() =>
        {
            try
            {
                var opCoDirs = Directory.GetDirectories(rootPath);
                var allStats = new List<OpCoStatItem>();

                foreach (var dirPath in opCoDirs)
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    var files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                    long totalSize = files.Sum(f => f.Length);
                    allStats.Add(new OpCoStatItem
                    {
                        OpCoName = dirInfo.Name,
                        FileCount = files.Length,
                        DeviceCount = totalSize
                    });

                    foreach (var file in files)
                    {
                        if (file.Length != 17)
                        {
                            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                            {
                                ProblematicFiles.Add(new ProblematicFile { Path = file.FullName });
                            });
                        }
                    }
                }

                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    TotalFileCount = allStats.Sum(s => s.FileCount);
                    TotalDeviceCount = allStats.Sum(s => s.DeviceCount);
                    ProblematicFileCount = ProblematicFiles.Count;

                    var fileSeries = allStats.Select(s => new PieSeries<int> { Name = s.OpCoName, Values = new int[] { s.FileCount } });
                    var deviceSeries = allStats.Select(s => new PieSeries<long> { Name = s.OpCoName, Values = new long[] { s.DeviceCount } });

                    foreach (var series in fileSeries) OpCoFileSeries.Add(series);
                    foreach (var series in deviceSeries) OpCoDeviceSeries.Add(series);
                });
            }
            catch (Exception ex)
            {
                _infoBarService.Show("Error loading stats", ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
        });
    }

    /// <summary>
    /// Opens a file in the preview page for detailed inspection.
    /// Command is public to allow binding from XAML.
    /// </summary>
    /// <param name="file">The problematic file to open.</param>
    [RelayCommand]
    public void OpenFile(ProblematicFile file)
    {
        if (file == null) return;
        var fileItem = new FileItem { Name = Path.GetFileName(file.Path), FullPath = file.Path };
        _navigationService.NavigateTo(typeof(PreviewPage), fileItem);
    }
}