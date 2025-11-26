using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Handles storage and retrieval of application settings. 
/// Note: With the new PIN-based system, sensitive data encryption is no longer needed
/// as the PIN is hardcoded and not stored. 
/// </summary>
public class EncryptedSettingsService
{
    private const string SECURITY_CONFIG_FILENAME = "security.config";

    public class SecurityData
    {
        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "PIN-based security system - PIN is hardcoded";
    }

    /// <summary>
    /// Get the security config file path based on user's chosen storage location
    /// </summary>
    public string GetSecurityConfigPath(string baseStoragePath)
    {
        if (string.IsNullOrWhiteSpace(baseStoragePath))
        {
            baseStoragePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIM"
            );
        }

        var configDir = Path.Combine(baseStoragePath, "Security");
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            Debug.WriteLine($"[EncryptedSettings] Created security config directory: {configDir}");
        }

        return Path.Combine(configDir, SECURITY_CONFIG_FILENAME);
    }

    /// <summary>
    /// Save security configuration metadata (no sensitive data stored with PIN-based system)
    /// </summary>
    public async Task SaveSecurityConfigAsync(string configPath)
    {
        try
        {
            var securityData = new SecurityData
            {
                LastModified = DateTime.UtcNow,
                Notes = "PIN-based security system - PIN is hardcoded for enhanced security"
            };

            var configDir = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var configJson = JsonSerializer.Serialize(securityData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(configPath, configJson);

            Debug.WriteLine($"[EncryptedSettings] Security config saved to: {configPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EncryptedSettings] ERROR saving security config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Load security configuration metadata
    /// </summary>
    public async Task<SecurityData> LoadSecurityConfigAsync(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                Debug.WriteLine($"[EncryptedSettings] Security config not found at: {configPath}");
                await SaveSecurityConfigAsync(configPath);
                return new SecurityData { LastModified = DateTime.UtcNow };
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            var securityData = JsonSerializer.Deserialize<SecurityData>(configJson);

            Debug.WriteLine($"[EncryptedSettings] Security config loaded successfully from: {configPath}");
            return securityData ?? new SecurityData();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EncryptedSettings] ERROR loading security config: {ex.Message}");
            return new SecurityData();
        }
    }
}