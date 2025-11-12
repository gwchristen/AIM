using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;

namespace AIM.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService; // NEW: We need the file service

    [ObservableProperty]
    private string selectedRoot;

    // --- THIS IS THE FIX ---
    // The LeftTree property that BrowseViewModel needs.
    public ObservableCollection<DirectoryItem> LeftTree { get; } = new();

    public ObservableCollection<FileItem> SelectedScanFiles { get; } = new();

    // UPDATED: Constructor now asks for IFileService as well
    public MainViewModel(ISettingsService settingsService, IFileService fileService)
    {
        _settingsService = settingsService;
        _fileService = fileService; // NEW: Store the file service

        var appSettings = _settingsService.LoadSettings();
        // Set the property, which will trigger the OnSelectedRootChanged method
        SelectedRoot = appSettings.DefaultRootDirectory;
    }

    partial void OnSelectedRootChanged(string value)
    {
        // Save the setting
        var appSettings = _settingsService.LoadSettings();
        appSettings.DefaultRootDirectory = value;
        _settingsService.SaveSettings(appSettings);

        // --- THIS IS THE FIX ---
        // Rebuild the directory tree whenever the root changes.
        BuildTree();
    }

    private void BuildTree()
    {
        LeftTree.Clear();
        if (string.IsNullOrEmpty(SelectedRoot) || !Directory.Exists(SelectedRoot))
        {
            return;
        }

        var rootNode = new DirectoryItem
        {
            Name = Path.GetFileName(SelectedRoot),
            FullPath = SelectedRoot
        };

        _fileService.PopulateSubDirectories(rootNode);
        LeftTree.Add(rootNode);
    }
}