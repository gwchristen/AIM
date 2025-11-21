using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;

namespace AIM.Services;

/// <summary>
/// Handles encryption and decryption of sensitive settings using Windows Data Protection
/// </summary>
public class EncryptedSettingsService : IEncryptedSettingsService
{
    private const string SECURITY_CONFIG_FILENAME = "security.config";

    public class EncryptedSecurityConfig
    {
        [JsonPropertyName("masterPasswordHash")]
        public string MasterPasswordHash { get; set; }

        [JsonPropertyName("authorizedUsers")]
        public List<string> AuthorizedUsers { get; set; } = new();

        [JsonPropertyName("encryptedData")]
        public string EncryptedData { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }
    }

    public class SecurityData
    {
        [JsonPropertyName("masterPassword")]
        public string MasterPassword { get; set; }

        [JsonPropertyName("authorizedUsers")]
        public List<string> AuthorizedUsers { get; set; } = new();
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
    /// Save encrypted security configuration
    /// </summary>
    public async Task SaveSecurityConfigAsync(string configPath, string masterPassword, List<string> authorizedUsers)
    {
        try
        {
            var securityData = new SecurityData
            {
                MasterPassword = masterPassword,
                AuthorizedUsers = authorizedUsers
            };

            // Serialize security data
            var json = JsonSerializer.Serialize(securityData);

            // Use Windows Data Protection (DPAPI)
            var provider = new DataProtectionProvider("LOCAL=user");
            IBuffer buffData = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
            IBuffer buffEncrypted = await provider.ProtectAsync(buffData);
            string encryptedData = CryptographicBuffer.EncodeToBase64String(buffEncrypted);

            // Hash the master password for verification
            string masterPasswordHash = HashPassword(masterPassword);

            var config = new EncryptedSecurityConfig
            {
                MasterPasswordHash = masterPasswordHash,
                AuthorizedUsers = authorizedUsers,
                EncryptedData = encryptedData,
                LastModified = DateTime.UtcNow
            };

            // Ensure directory exists
            var configDir = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // Write to file
            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
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
    /// Load and decrypt security configuration
    /// </summary>
    public async Task<SecurityData> LoadSecurityConfigAsync(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                Debug.WriteLine($"[EncryptedSettings] Security config not found at: {configPath}");
                return null;
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<EncryptedSecurityConfig>(configJson);

            if (config == null)
            {
                Debug.WriteLine($"[EncryptedSettings] Failed to deserialize security config");
                return null;
            }

            // Decrypt data using DPAPI
            var provider = new DataProtectionProvider("LOCAL=user");
            IBuffer buffEncrypted = CryptographicBuffer.DecodeFromBase64String(config.EncryptedData);
            IBuffer buffDecrypted = await provider.UnprotectAsync(buffEncrypted);
            string json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffDecrypted);

            var securityData = JsonSerializer.Deserialize<SecurityData>(json);

            Debug.WriteLine($"[EncryptedSettings] Security config loaded successfully from: {configPath}");
            return securityData;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EncryptedSettings] ERROR loading security config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Hash password using SHA256
    /// </summary>
    private string HashPassword(string password)
    {
        var buffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
        var hashedBuffer = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm(
            Windows.Security.Cryptography.Core.HashAlgorithmNames.Sha256).HashData(buffer);
        return CryptographicBuffer.EncodeToBase64String(hashedBuffer);
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    public bool VerifyPasswordHash(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }
}