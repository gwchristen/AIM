using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string totalTextFilesText;

    [ObservableProperty]
    private string totalLinesText;

    [ObservableProperty]
    private string ohioFilesText;

    [ObservableProperty]
    private string ohioLinesText;

    [ObservableProperty]
    private string imFilesText;

    [ObservableProperty]
    private string imLinesText;

    [ObservableProperty]
    private ObservableCollection<ProblematicFile> problematicFiles = new();

    public StatsViewModel(MainViewModel mainViewModel, INavigationService navigationService)
    {
        _mainViewModel = mainViewModel;
        _navigationService = navigationService;
        LoadStats();
    }

    private async Task LoadStats()
    {
        await Task.Run(() =>
        {
            string totalTextFiles = "Total Text Files: 0";
            string totalLines = "Total Non-Blank Lines: 0";
            string ohioFiles = "Ohio Files: 0";
            string ohioLines = "Ohio Lines: 0";
            string imFiles = "I&M Files: 0";
            string imLines = "I&M Lines: 0";
            var probFiles = new ObservableCollection<ProblematicFile>();

            if (string.IsNullOrEmpty(_mainViewModel.SelectedRoot) || !Directory.Exists(_mainViewModel.SelectedRoot))
            {
                // Directory doesn't exist, use default values.
            }
            else
            {
                var textFiles = Directory.EnumerateFiles(_mainViewModel.SelectedRoot, "*.txt", SearchOption.AllDirectories).ToList();
                var totalFiles = textFiles.Count;
                totalTextFiles = $"Total Text Files: {totalFiles}";

                var ohioDir = Path.Combine(_mainViewModel.SelectedRoot, "Ohio");
                var imDir = Path.Combine(_mainViewModel.SelectedRoot, "I&M");
                var ohioFileCount = Directory.Exists(ohioDir) ? Directory.EnumerateFiles(ohioDir, "*.txt", SearchOption.AllDirectories).Count() : 0;
                var imFileCount = Directory.Exists(imDir) ? Directory.EnumerateFiles(imDir, "*.txt", SearchOption.AllDirectories).Count() : 0;
                ohioFiles = $"Ohio Files: {ohioFileCount}";
                imFiles = $"I&M Files: {imFileCount}";

                var totalLineCount = textFiles.Sum(f => File.ReadAllLines(f).Count(l => !string.IsNullOrWhiteSpace(l)));
                totalLines = $"Total Devices: {totalLineCount}";

                var ohioLineCount = Directory.Exists(ohioDir) ? Directory.EnumerateFiles(ohioDir, "*.txt", SearchOption.AllDirectories).Sum(f => File.ReadAllLines(f).Count(l => !string.IsNullOrWhiteSpace(l))) : 0;
                var imLineCount = Directory.Exists(imDir) ? Directory.EnumerateFiles(imDir, "*.txt", SearchOption.AllDirectories).Sum(f => File.ReadAllLines(f).Count(l => !string.IsNullOrWhiteSpace(l))) : 0;
                ohioLines = $"Ohio Devices: {ohioLineCount}";
                imLines = $"I&M Devices: {imLineCount}";

                foreach (var file in textFiles)
                {
                    var lines = File.ReadAllLines(file);
                    if (lines.Any(l => !string.IsNullOrWhiteSpace(l) && l.Length != 17))
                    {
                        probFiles.Add(new ProblematicFile { Path = file });
                    }
                }
            }

            // ** THIS IS THE CORRECTED SECTION **
            // Use the DispatcherQueue from the main window to safely update UI properties.
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                TotalTextFilesText = totalTextFiles;
                TotalLinesText = totalLines;
                OhioFilesText = ohioFiles;
                OhioLinesText = ohioLines;
                ImFilesText = imFiles;
                ImLinesText = imLines;
                ProblematicFiles = probFiles;
            });
        });
    }

    [RelayCommand]
    public void OpenFile(ProblematicFile file)
    {
        if (file == null) return;

        var fileItem = new FileItem
        {
            FullPath = file.Path,
            Name = Path.GetFileName(file.Path),
            Type = GetFileType(file.Path)
        };

        _navigationService.NavigateTo(typeof(Views.PreviewPage), fileItem);
    }

    private FileType GetFileType(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".txt" => FileType.Text,
            ".csv" => FileType.Csv,
            ".log" => FileType.Log,
            _ => FileType.Other
        };
    }
}

public class ProblematicFile
{
    public string Path { get; set; } = string.Empty;
}