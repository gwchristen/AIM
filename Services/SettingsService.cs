using AIM.Models;
using System;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace AIM.Services;

public class SettingsService : ISettingsService
{
    private string _settingsPath;
    private bool _initialized = false;

    /// <summary>
    /// Lazy initialization of settings path.
    /// This avoids accessing ApplicationData.Current too early in the app lifecycle.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;

        try
        {
            var appDataFolder = ApplicationData.Current.LocalFolder.Path;
            _settingsPath = Path.Combine(appDataFolder, "settings.json");
            _initialized = true;
        }
        catch
        {
            // Fallback to user's local app data if ApplicationData.Current fails
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _settingsPath = Path.Combine(localAppData, "AIM", "settings.json");

            // Ensure directory exists
            var settingsDir = Path.GetDirectoryName(_settingsPath);
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            _initialized = true;
        }
    }

    public AppSettings LoadSettings()
    {
        try
        {
            EnsureInitialized();

            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Error loading settings: {ex.Message}");
            // If deserialization fails, return default settings
        }

        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            EnsureInitialized();

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Error saving settings: {ex.Message}");
        }
    }
}