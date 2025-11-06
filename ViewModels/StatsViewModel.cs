using AIM.Models;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

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

    public StatsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
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

            if (!Directory.Exists(_mainViewModel.SelectedRoot))
            {
                // Use defaults
            }
            else
            {
                // Get all text files in root
                var textFiles = Directory.EnumerateFiles(_mainViewModel.SelectedRoot, "*.txt", SearchOption.AllDirectories).ToList();
                var totalFiles = textFiles.Count;
                totalTextFiles = $"Total Text Files: {totalFiles}";

                // Subdirs
                var ohioDir = Path.Combine(_mainViewModel.SelectedRoot, "Ohio");
                var imDir = Path.Combine(_mainViewModel.SelectedRoot, "I&M");
                var ohioFileCount = Directory.Exists(ohioDir) ? Directory.EnumerateFiles(ohioDir, "*.txt", SearchOption.AllDirectories).Count() : 0;
                var imFileCount = Directory.Exists(imDir) ? Directory.EnumerateFiles(imDir, "*.txt", SearchOption.AllDirectories).Count() : 0;
                ohioFiles = $"Ohio Files: {ohioFileCount}";
                imFiles = $"I&M Files: {imFileCount}";

                // Total lines (non-blank)
                var totalLineCount = textFiles.Sum(f => File.ReadAllLines(f).Count(l => !string.IsNullOrWhiteSpace(l)));
                totalLines = $"Total Devices: {totalLineCount}";

                // Lines per subdir
                var ohioLineCount = Directory.Exists(ohioDir) ? Directory.EnumerateFiles(ohioDir, "*.txt", SearchOption.AllDirectories).Sum(f => File.ReadAllLines(f).Count(l => !string.IsNullOrWhiteSpace(l))) : 0;
                var imLineCount = Directory.Exists(imDir) ? Directory.EnumerateFiles(imDir, "*.txt", SearchOption.AllDirectories).Sum(f => File.ReadAllLines(f).Count(l => !string.IsNullOrWhiteSpace(l))) : 0;
                ohioLines = $"Ohio Devices: {ohioLineCount}";
                imLines = $"I&M Devices: {imLineCount}";

                // Problematic files (lines not equal to 17 chars)
                foreach (var file in textFiles)
                {
                    var lines = File.ReadAllLines(file);
                    if (lines.Any(l => !string.IsNullOrWhiteSpace(l) && l.Length != 17))
                    {
                        probFiles.Add(new ProblematicFile { Path = file });
                    }
                }
            }

            // Update UI on main thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
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
    public async Task OpenFile(ProblematicFile file)
    {
        // Navigate to Preview tab and load file
        if (MainWindow.Instance != null)
        {
            // MainWindow.Instance.MainFrame.Navigate(typeof(PreviewPage));
            // Set the selected tab
            MainWindow.Instance.IsPreviewSelected = true;
            MainWindow.Instance.IsBrowseSelected = false;
            MainWindow.Instance.IsSearchSelected = false;
            MainWindow.Instance.IsScansSelected = false;
            MainWindow.Instance.IsInvArchivesSelected = false;
            MainWindow.Instance.IsStatsSelected = false;
            MainWindow.Instance.IsSettingsSelected = false;

            // TODO: Implement navigation in Avalonia
            // Load the file in Preview
            // if (MainWindow.Instance.MainFrame.Content is PreviewPage previewPage)
            // {
            //     var fileItem = new FileItem
            //     {
            //         FullPath = file.Path,
            //         Name = Path.GetFileName(file.Path),
            //         Type = GetFileType(file.Path)
            //     };
            //     await previewPage.ViewModel.LoadFileContent(fileItem);
            // }
            await Task.CompletedTask;
        }
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