using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIM.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private AppSettings _appSettings;

    // These are the properties the UI will bind to.
    [ObservableProperty]
    private string defaultRootDirectory;

    [ObservableProperty]
    private string archivePath;

    [ObservableProperty]
    private string shippedDirectory;

    [ObservableProperty]
    private string fileScansDirectory;

    [ObservableProperty]
    private string inventoryArchiveDirectory;

    [ObservableProperty]
    private string password;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        _appSettings = _settingsService.LoadSettings();

        // Populate the ViewModel properties from the loaded settings model
        DefaultRootDirectory = _appSettings.DefaultRootDirectory;
        ArchivePath = _appSettings.ArchivePath;
        ShippedDirectory = _appSettings.ShippedDirectory;
        FileScansDirectory = _appSettings.FileScansDirectory;
        InventoryArchiveDirectory = _appSettings.InventoryArchiveDirectory;
        Password = _appSettings.Password;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        // Update the settings model from the ViewModel properties
        _appSettings.DefaultRootDirectory = DefaultRootDirectory;
        _appSettings.ArchivePath = ArchivePath;
        _appSettings.ShippedDirectory = ShippedDirectory;
        _appSettings.FileScansDirectory = FileScansDirectory;
        _appSettings.InventoryArchiveDirectory = InventoryArchiveDirectory;
        _appSettings.Password = Password;

        // Save the updated model
        _settingsService.SaveSettings(_appSettings);
    }
}