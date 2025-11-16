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
    public ObservableCollection<FileItem> SelectedScanFiles { get; } = new();

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
    /// Loads authorized users from settings and updates SecurityService.
    /// This must be called early, before checking authorization status.
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
    /// Updates the Inventory tab visibility based on current security status.
    /// This should be called whenever the security status changes (e.g., master password override activated/deactivated).
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