using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
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

    public DirAnalysisViewModel(IDialogService dialogService, DirectoryOperationService directoryOperationService)
    {
        _dialogService = dialogService;
        _directoryOperationService = directoryOperationService;

        _opCoStats = new ObservableCollection<OpCoStatItem>();
        _misplacedOhFiles = new ObservableCollection<string>();
        _misplacedImFiles = new ObservableCollection<string>();
        _unidentifiedFiles = new ObservableCollection<string>();
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
            // Run both operations concurrently
            var statsTask = _directoryOperationService.GetDirectoryStatsAsync(AnalysisDirectory);
            var anomalyTask = _directoryOperationService.FindFileAnomaliesAsync(AnalysisDirectory);

            await Task.WhenAll(statsTask, anomalyTask);

            var stats = await statsTask;
            foreach (var stat in stats) OpCoStats.Add(stat);

            var report = await anomalyTask;
            report.MisplacedOhFiles.ForEach(MisplacedOhFiles.Add);
            report.MisplacedImFiles.ForEach(MisplacedImFiles.Add);
            report.UnidentifiedFiles.ForEach(UnidentifiedFiles.Add);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Analysis Failed", $"An error occurred.\nError: {ex.Message}");
        }
    }

    private void ClearResults()
    {
        OpCoStats.Clear();
        MisplacedOhFiles.Clear();
        MisplacedImFiles.Clear();
        UnidentifiedFiles.Clear();
    }

    private async Task<string?> PickFolderAsync()
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