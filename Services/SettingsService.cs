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
        var appDataFolder = ApplicationData.Current.LocalFolder.Path;
        _settingsPath = Path.Combine(appDataFolder, "settings.json");
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