using AIM.Models;
using System;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace AIM.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appDataFolder = GetAppDataFolder();
        _settingsPath = Path.Combine(appDataFolder, "settings.json");

        // Ensure the directory exists
        Directory.CreateDirectory(appDataFolder);
    }

    private static string GetAppDataFolder()
    {
        // Check if running as a packaged app (MSIX)
        if (IsPackaged())
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            // Fallback for unpackaged:  use LocalApplicationData
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "AIM");
        }
    }

    private static bool IsPackaged()
    {
        try
        {
            // This will throw if not packaged
            _ = Windows.ApplicationModel.Package.Current.Id;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // This now correctly returns the AppSettings object, fixing the error.
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // If deserialization fails, return default settings
        }
        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception)
        {
            // Handle save error if necessary
        }
    }
}