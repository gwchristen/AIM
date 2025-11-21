using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace AIM.ViewModels;

/// <summary>
/// Main application view model that manages the primary application state.
/// Handles directory tree navigation and file selection.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;

    /// <summary>
    /// Gets or sets the currently selected root directory path.
    /// When changed, triggers rebuilding of the directory tree.
    /// </summary>
    [ObservableProperty]
    private string selectedRoot;

    /// <summary>
    /// Gets the collection of directory items for the left navigation tree.
    /// Populated based on the selected root directory.
    /// </summary>
    public ObservableCollection<DirectoryItem> LeftTree { get; } = new();
    
    /// <summary>
    /// Gets the collection of selected scan files.
    /// Used for file operations and previews.
    /// </summary>
    public ObservableCollection<FileItem> SelectedScanFiles { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// Loads settings and sets up the initial application state.
    /// </summary>
    /// <param name="settingsService">Service for loading and saving application settings.</param>
    /// <param name="fileService">Service for file and directory operations.</param>
    public MainViewModel(ISettingsService settingsService, IFileService fileService)
    {
        _settingsService = settingsService;
        _fileService = fileService;

        Debug.WriteLine($"[MainViewModel] Constructor starting");

        // Load the selected root from settings
        var appSettings = _settingsService.LoadSettings();
        SelectedRoot = appSettings.DefaultRootDirectory;

        Debug.WriteLine($"[MainViewModel] MainViewModel initialized");
    }

    /// <summary>
    /// Partial method invoked when the selected root directory changes.
    /// Saves the new root to settings and rebuilds the directory tree.
    /// </summary>
    /// <param name="value">The new selected root directory path.</param>
    partial void OnSelectedRootChanged(string value)
    {
        var appSettings = _settingsService.LoadSettings();
        appSettings.DefaultRootDirectory = value;
        _settingsService.SaveSettings(appSettings);

        BuildTree();
    }

    /// <summary>
    /// Builds the directory tree from the currently selected root directory.
    /// Clears existing tree and populates subdirectories recursively.
    /// </summary>
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