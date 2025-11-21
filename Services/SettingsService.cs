using AIM.Models;
using System;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace AIM.Services;

/// <summary>
/// Exception thrown when settings.json file is not found.
/// </summary>
public class SettingsNotFoundException : Exception
{
    public SettingsNotFoundException(string message) : base(message) { }
    public SettingsNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when settings.json file exists but is corrupted or invalid.
/// </summary>
public class SettingsCorruptedException : Exception
{
    public SettingsCorruptedException(string message) : base(message) { }
    public SettingsCorruptedException(string message, Exception innerException) : base(message, innerException) { }
}

public class SettingsService : ISettingsService
{
    private string _settingsPath;
    private bool _initialized = false;

    /// <summary>
    /// Gets the canonical settings path: %LOCALAPPDATA%\AIM\settings.json
    /// This path is shared between the installer and the application.
    /// </summary>
    public static string GetCanonicalSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "AIM", "settings.json");
    }

    /// <summary>
    /// Gets the legacy WinUI settings path for migration purposes.
    /// </summary>
    private static string GetLegacySettingsPath()
    {
        try
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Lazy initialization of settings path.
    /// Uses canonical path: %LOCALAPPDATA%\AIM\settings.json
    /// Migrates from legacy WinUI path if needed.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;

        // Always use canonical path
        _settingsPath = GetCanonicalSettingsPath();

        // Ensure directory exists
        var settingsDir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }

        // Migrate from legacy WinUI path if it exists and canonical doesn't
        var legacyPath = GetLegacySettingsPath();
        if (!File.Exists(_settingsPath) && !string.IsNullOrEmpty(legacyPath) && File.Exists(legacyPath))
        {
            try
            {
                File.Copy(legacyPath, _settingsPath, overwrite: false);
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Migrated settings from {legacyPath} to {_settingsPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Warning: Could not migrate legacy settings: {ex.Message}");
            }
        }

        _initialized = true;
    }

    /// <summary>
    /// Loads application settings from the canonical settings file.
    /// Throws SettingsNotFoundException if settings file is missing.
    /// Throws SettingsCorruptedException if settings file is corrupted.
    /// </summary>
    /// <exception cref="SettingsNotFoundException">Thrown when settings.json does not exist</exception>
    /// <exception cref="SettingsCorruptedException">Thrown when settings.json cannot be deserialized</exception>
    public AppSettings LoadSettings()
    {
        try
        {
            EnsureInitialized();

            if (!File.Exists(_settingsPath))
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] ERROR: Settings file not found at {_settingsPath}");
                throw new SettingsNotFoundException($"Settings file not found at {_settingsPath}. Please run the AIM installer to initialize the application.");
            }

            var json = File.ReadAllText(_settingsPath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] ERROR: Settings file is empty at {_settingsPath}");
                throw new SettingsCorruptedException($"Settings file is empty at {_settingsPath}. Please reinstall AIM or restore from backup.");
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings == null)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] ERROR: Failed to deserialize settings from {_settingsPath}");
                throw new SettingsCorruptedException($"Settings file is corrupted at {_settingsPath}. Please reinstall AIM or restore from backup.");
            }

            System.Diagnostics.Debug.WriteLine($"[SettingsService] Settings loaded successfully from {_settingsPath}");
            return settings;
        }
        catch (SettingsNotFoundException)
        {
            throw; // Re-throw custom exceptions
        }
        catch (SettingsCorruptedException)
        {
            throw; // Re-throw custom exceptions
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] ERROR: JSON deserialization failed: {ex.Message}");
            throw new SettingsCorruptedException($"Settings file is corrupted at {_settingsPath}: {ex.Message}. Please reinstall AIM or restore from backup.", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] ERROR: Unexpected error loading settings: {ex.Message}");
            throw new SettingsCorruptedException($"Error loading settings from {_settingsPath}: {ex.Message}. Please reinstall AIM or contact support.", ex);
        }
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