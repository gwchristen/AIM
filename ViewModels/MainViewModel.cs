using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace AIM.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;
    private readonly SecurityService _securityService;

    [ObservableProperty]
    private string selectedRoot;

    [ObservableProperty]
    private bool isInventoryTabVisible;

    // Collections required by other ViewModels
    public ObservableCollection<DirectoryItem> LeftTree { get; } = new();

    private ObservableCollection<FileItem> _selectedScanFiles = new();
    public ObservableCollection<FileItem> SelectedScanFiles
    {
        get => _selectedScanFiles;
    }

    /// <summary>
    /// Constructor that accepts all required services.  
    /// The DI container will use this single constructor.
    /// </summary>
    public MainViewModel(ISettingsService settingsService, IFileService fileService, SecurityService securityService)
    {
        _settingsService = settingsService;
        _fileService = fileService;
        _securityService = securityService;

        Debug.WriteLine($"[MainViewModel] Constructor starting");

        // Load the selected root from settings
        var appSettings = _settingsService.LoadSettings();
        SelectedRoot = appSettings.DefaultRootDirectory;

        // Check inventory visibility (based on current PIN unlock status)
        UpdateInventoryTabVisibility();

        Debug.WriteLine($"[Main] MainViewModel initialized");
        Debug.WriteLine($"[Main] Current user: {_securityService.CurrentUserId}");
        Debug.WriteLine($"[Main] Is fully unlocked: {_securityService.IsFullyUnlocked}");
        Debug.WriteLine($"[Main] Inventory tab visible: {IsInventoryTabVisible}");

        // Add debug logging to SelectedScanFiles collection changes
        _selectedScanFiles.CollectionChanged += (s, e) =>
        {
            Debug.WriteLine($"[MainViewModel.SelectedScanFiles] CollectionChanged - Action: {e.Action}, Count: {_selectedScanFiles.Count}");
            Debug.WriteLine($"[MainViewModel. SelectedScanFiles] Stack: {System.Environment.StackTrace}");
        };
    }

    /// <summary>
    /// Updates the Inventory tab visibility based on current security status. 
    /// This should be called whenever the security status changes (e.g., PIN unlock activated/deactivated).
    /// </summary>
    public void UpdateInventoryTabVisibility()
    {
        bool newValue = _securityService.IsFullyUnlocked;
        Debug.WriteLine($"[MainViewModel] UpdateInventoryTabVisibility called. Current: {IsInventoryTabVisible}, New: {newValue}");

        IsInventoryTabVisible = newValue;

        Debug.WriteLine($"[MainViewModel] IsInventoryTabVisible set to: {IsInventoryTabVisible}");
    }

    /// <summary>
    /// Called when the selected root directory changes. 
    /// </summary>
    partial void OnSelectedRootChanged(string value)
    {
        var appSettings = _settingsService.LoadSettings();
        appSettings.DefaultRootDirectory = value;
        _settingsService.SaveSettings(appSettings);

        BuildTree();
    }

    /// <summary>
    /// Builds the directory tree from the selected root.  
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