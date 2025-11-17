using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace AIM.ViewModels;

/// <summary>
/// Main application view model that manages the primary application state.
/// Handles directory tree navigation, file selection, and security-based feature visibility.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;
    private readonly SecurityService _securityService;

    /// <summary>
    /// Gets or sets the currently selected root directory path.
    /// When changed, triggers rebuilding of the directory tree.
    /// </summary>
    [ObservableProperty]
    private string selectedRoot;

    /// <summary>
    /// Gets or sets whether the Inventory tab is visible in the navigation.
    /// Visibility is controlled by user authorization status.
    /// </summary>
    [ObservableProperty]
    private bool isInventoryTabVisible;

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
    /// Loads settings, initializes security, and sets up the initial application state.
    /// </summary>
    /// <param name="settingsService">Service for loading and saving application settings.</param>
    /// <param name="fileService">Service for file and directory operations.</param>
    /// <param name="securityService">Service for managing security and authorization.</param>
    public MainViewModel(ISettingsService settingsService, IFileService fileService, SecurityService securityService)
    {
        _settingsService = settingsService;
        _fileService = fileService;
        _securityService = securityService;

        Debug.WriteLine($"[MainViewModel] Constructor starting");

        // IMPORTANT: Load authorized users from settings FIRST
        LoadAuthorizedUsersFromSettings();

        // Set default master password
        _securityService.SetMasterPassword("AIMAdmin123");

        // Load the selected root from settings
        var appSettings = _settingsService.LoadSettings();
        SelectedRoot = appSettings.DefaultRootDirectory;

        // NOW check inventory visibility (after authorized users are loaded)
        UpdateInventoryTabVisibility();

        Debug.WriteLine($"[Main] MainViewModel initialized");
        Debug.WriteLine($"[Main] Current user: {_securityService.CurrentUserId}");
        Debug.WriteLine($"[Main] Is fully unlocked: {_securityService.IsFullyUnlocked}");
        Debug.WriteLine($"[Main] Inventory tab visible: {IsInventoryTabVisible}");
    }

    /// <summary>
    /// Loads authorized users from application settings and updates the security service.
    /// This method must be called early in initialization, before checking authorization status.
    /// </summary>
    private void LoadAuthorizedUsersFromSettings()
    {
        var appSettings = _settingsService.LoadSettings();
        var authorizedUsers = appSettings?.AuthorizedUsers ?? new System.Collections.Generic.List<string>();

        Debug.WriteLine($"[MainViewModel] Loaded {authorizedUsers.Count} authorized users from settings");
        foreach (var user in authorizedUsers)
        {
            Debug.WriteLine($"[MainViewModel]   - {user}");
        }

        // Set the authorized users list in SecurityService
        _securityService.SetAuthorizedUsers(authorizedUsers);
        Debug.WriteLine($"[MainViewModel] SecurityService.IsFullyUnlocked after loading: {_securityService.IsFullyUnlocked}");
    }

    /// <summary>
    /// Updates the visibility of the Inventory tab based on current security status.
    /// Should be called whenever the security status changes (e.g., master password override activated/deactivated).
    /// </summary>
    public void UpdateInventoryTabVisibility()
    {
        bool newValue = _securityService.IsFullyUnlocked;
        Debug.WriteLine($"[MainViewModel] UpdateInventoryTabVisibility called. Current: {IsInventoryTabVisible}, New: {newValue}");

        IsInventoryTabVisible = newValue;

        Debug.WriteLine($"[MainViewModel] IsInventoryTabVisible set to: {IsInventoryTabVisible}");
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