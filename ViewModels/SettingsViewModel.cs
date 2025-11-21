using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AIM.ViewModels;

/// <summary>
/// ViewModel for the Settings page, managing application configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IThemeService _themeService;
    private readonly ILockService _lockService;
    private readonly IAuditLoggingService _auditLoggingService;
    private AppSettings _appSettings;

    // Directory Settings Properties
    
    /// <summary>
    /// Gets or sets the default root directory for file browsing operations.
    /// </summary>
    [ObservableProperty]
    private string defaultRootDirectory;

    /// <summary>
    /// Gets or sets the path where archived files are stored.
    /// </summary>
    [ObservableProperty]
    private string archivePath;

    /// <summary>
    /// Gets or sets the directory path for shipped items.
    /// </summary>
    [ObservableProperty]
    private string shippedDirectory;

    /// <summary>
    /// Gets or sets the directory where file scan results are stored.
    /// </summary>
    [ObservableProperty]
    private string fileScansDirectory;

    /// <summary>
    /// Gets or sets the directory where inventory archives are stored.
    /// </summary>
    [ObservableProperty]
    private string inventoryArchiveDirectory;

    // Theme Properties

    /// <summary>
    /// Gets or sets the selected theme.
    /// </summary>
    [ObservableProperty]
    private string selectedTheme;

    /// <summary>
    /// Gets whether directory controls are enabled (not locked).
    /// </summary>
    [ObservableProperty]
    private bool areDirectoryControlsEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(
        ISettingsService settingsService,
        IDialogService dialogService,
        IThemeService themeService,
        ILockService lockService,
        IAuditLoggingService auditLoggingService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        _themeService = themeService;
        _lockService = lockService;
        _auditLoggingService = auditLoggingService;

        // Subscribe to lock state changes
        _lockService.LockStateChanged += OnLockStateChanged;
        AreDirectoryControlsEnabled = !_lockService.IsLocked;

        LoadSettings();
    }

    private void OnLockStateChanged(object? sender, bool isLocked)
    {
        AreDirectoryControlsEnabled = !isLocked;
        Debug.WriteLine($"[SettingsViewModel] Directory controls enabled: {AreDirectoryControlsEnabled}");
    }

    /// <summary>
    /// Loads application settings.
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            _appSettings = _settingsService.LoadSettings();

            DefaultRootDirectory = _appSettings.DefaultRootDirectory;
            ArchivePath = _appSettings.ArchivePath;
            ShippedDirectory = _appSettings.ShippedDirectory;
            FileScansDirectory = _appSettings.FileScansDirectory;
            InventoryArchiveDirectory = _appSettings.InventoryArchiveDirectory;
            SelectedTheme = _appSettings.Theme;

            Debug.WriteLine("[SettingsViewModel] Settings loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsViewModel] Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            var oldSettings = new 
            {
                DefaultRootDirectory = _appSettings.DefaultRootDirectory,
                ArchivePath = _appSettings.ArchivePath,
                ShippedDirectory = _appSettings.ShippedDirectory,
                FileScansDirectory = _appSettings.FileScansDirectory,
                InventoryArchiveDirectory = _appSettings.InventoryArchiveDirectory,
                Theme = _appSettings.Theme
            };

            _appSettings.DefaultRootDirectory = DefaultRootDirectory;
            _appSettings.ArchivePath = ArchivePath;
            _appSettings.ShippedDirectory = ShippedDirectory;
            _appSettings.FileScansDirectory = FileScansDirectory;
            _appSettings.InventoryArchiveDirectory = InventoryArchiveDirectory;
            _appSettings.Theme = SelectedTheme;

            _settingsService.SaveSettings(_appSettings);

            Debug.WriteLine("[SettingsViewModel] Settings saved successfully");

            // Log settings changes
            var changes = new System.Collections.Generic.Dictionary<string, string>();
            if (oldSettings.DefaultRootDirectory != DefaultRootDirectory)
                changes["DefaultRootDirectory"] = $"{oldSettings.DefaultRootDirectory} → {DefaultRootDirectory}";
            if (oldSettings.ArchivePath != ArchivePath)
                changes["ArchivePath"] = $"{oldSettings.ArchivePath} → {ArchivePath}";
            if (oldSettings.ShippedDirectory != ShippedDirectory)
                changes["ShippedDirectory"] = $"{oldSettings.ShippedDirectory} → {ShippedDirectory}";
            if (oldSettings.FileScansDirectory != FileScansDirectory)
                changes["FileScansDirectory"] = $"{oldSettings.FileScansDirectory} → {FileScansDirectory}";
            if (oldSettings.InventoryArchiveDirectory != InventoryArchiveDirectory)
                changes["InventoryArchiveDirectory"] = $"{oldSettings.InventoryArchiveDirectory} → {InventoryArchiveDirectory}";
            if (oldSettings.Theme != SelectedTheme)
                changes["Theme"] = $"{oldSettings.Theme} → {SelectedTheme}";

            if (changes.Count > 0)
            {
                _auditLoggingService.LogAudit(
                    "SETTINGS_CHANGED",
                    null,
                    $"User modified {changes.Count} setting(s)",
                    changes
                );
            }

            await _dialogService.ShowMessageAsync("Success", "Settings saved successfully.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsViewModel] Error saving settings: {ex.Message}");
            await _dialogService.ShowMessageAsync("Error", $"Failed to save settings: {ex.Message}");
            
            _auditLoggingService.LogAudit(
                "SETTINGS_SAVE_FAILED",
                null,
                $"Failed to save settings: {ex.Message}",
                new System.Collections.Generic.Dictionary<string, string> { { "error", ex.Message } }
            );
        }
    }

    /// <summary>
    /// Handles changes to the selected theme.
    /// </summary>
    partial void OnSelectedThemeChanged(string value)
    {
        if (_themeService != null && !string.IsNullOrEmpty(value))
        {
            _themeService.SetTheme(value);
            Debug.WriteLine($"[SettingsViewModel] Theme changed to: {value}");
            
            _auditLoggingService.LogAudit(
                "THEME_CHANGED",
                null,
                $"Theme changed to {value}"
            );
        }
    }

    /// <summary>
    /// Selects a directory for the default root directory.
    /// </summary>
    [RelayCommand]
    private async Task SelectDefaultRootDirectoryAsync()
    {
        var selectedPath = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            DefaultRootDirectory = selectedPath;
        }
    }

    /// <summary>
    /// Selects a directory for the archive path.
    /// </summary>
    [RelayCommand]
    private async Task SelectArchivePathAsync()
    {
        var selectedPath = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            ArchivePath = selectedPath;
        }
    }

    /// <summary>
    /// Selects a directory for the shipped directory.
    /// </summary>
    [RelayCommand]
    private async Task SelectShippedDirectoryAsync()
    {
        var selectedPath = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            ShippedDirectory = selectedPath;
        }
    }

    /// <summary>
    /// Selects a directory for the file scans directory.
    /// </summary>
    [RelayCommand]
    private async Task SelectFileScansDirectoryAsync()
    {
        var selectedPath = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            FileScansDirectory = selectedPath;
        }
    }

    /// <summary>
    /// Selects a directory for the inventory archive directory.
    /// </summary>
    [RelayCommand]
    private async Task SelectInventoryArchiveDirectoryAsync()
    {
        var selectedPath = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            InventoryArchiveDirectory = selectedPath;
        }
    }
}
