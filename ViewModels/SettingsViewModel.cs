using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace AIM.ViewModels;

/// <summary>
/// ViewModel for the Settings page, managing application configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IThemeService _themeService;
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
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(
        ISettingsService settingsService,
        IDialogService dialogService,
        IThemeService themeService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        _themeService = themeService;

        LoadSettings();
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
    private async void SaveSettings()
    {
        try
        {
            _appSettings.DefaultRootDirectory = DefaultRootDirectory;
            _appSettings.ArchivePath = ArchivePath;
            _appSettings.ShippedDirectory = ShippedDirectory;
            _appSettings.FileScansDirectory = FileScansDirectory;
            _appSettings.InventoryArchiveDirectory = InventoryArchiveDirectory;
            _appSettings.Theme = SelectedTheme;

            _settingsService.SaveSettings(_appSettings);

            Debug.WriteLine("[SettingsViewModel] Settings saved successfully");

            await _dialogService.ShowMessageAsync("Success", "Settings saved successfully.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsViewModel] Error saving settings: {ex.Message}");
            await _dialogService.ShowMessageAsync("Error", $"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles changes to the selected theme.
    /// </summary>
    partial void OnSelectedThemeChanged(string value)
    {
        if (_themeService != null)
        {
            _themeService.SetTheme(value);
            Debug.WriteLine($"[SettingsViewModel] Theme changed to: {value}");
        }
    }

    /// <summary>
    /// Selects a directory for the default root directory.
    /// </summary>
    [RelayCommand]
    private async void SelectDefaultRootDirectory()
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
    private async void SelectArchivePath()
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
    private async void SelectShippedDirectory()
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
    private async void SelectFileScansDirectory()
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
    private async void SelectInventoryArchiveDirectory()
    {
        var selectedPath = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            InventoryArchiveDirectory = selectedPath;
        }
    }
}
