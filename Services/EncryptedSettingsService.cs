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

        [JsonPropertyName("encryptionMode")]
        public string EncryptionMode { get; set; } = "dpapi"; // "dpapi" or "passphrase"
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
    public async Task SaveSecurityConfigAsync(string configPath, string masterPassword, List<string> authorizedUsers, string? passphrase = null)
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

            string encryptedData;
            string encryptionMode;

            if (!string.IsNullOrEmpty(passphrase))
            {
                // Use passphrase-based AES-GCM encryption
                encryptedData = EncryptWithPassphrase(json, passphrase);
                encryptionMode = "passphrase";
                Debug.WriteLine("[EncryptedSettings] Using passphrase-based encryption");
            }
            else
            {
                // Use Windows Data Protection (DPAPI)
                var provider = new DataProtectionProvider("LOCAL=user");
                IBuffer buffData = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
                IBuffer buffEncrypted = await provider.ProtectAsync(buffData);
                encryptedData = CryptographicBuffer.EncodeToBase64String(buffEncrypted);
                encryptionMode = "dpapi";
                Debug.WriteLine("[EncryptedSettings] Using DPAPI encryption");
            }

            // Hash the master password for verification
            string masterPasswordHash = HashPassword(masterPassword);

            var config = new EncryptedSecurityConfig
            {
                MasterPasswordHash = masterPasswordHash,
                AuthorizedUsers = authorizedUsers,
                EncryptedData = encryptedData,
                LastModified = DateTime.UtcNow,
                EncryptionMode = encryptionMode
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
    public async Task<SecurityData> LoadSecurityConfigAsync(string configPath, string? passphrase = null)
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

            string json;

            // Determine decryption method based on EncryptionMode
            if (config.EncryptionMode == "passphrase")
            {
                if (string.IsNullOrEmpty(passphrase))
                {
                    throw new InvalidOperationException(
                        "Security configuration is encrypted with a passphrase, but no passphrase was provided. " +
                        "Please configure the Passphrase in application settings.");
                }

                try
                {
                    json = DecryptWithPassphrase(config.EncryptedData, passphrase);
                    Debug.WriteLine("[EncryptedSettings] Successfully decrypted using passphrase");
                }
                catch (CryptographicException)
                {
                    throw new InvalidOperationException(
                        "Failed to decrypt security configuration. The passphrase may be incorrect.");
                }
            }
            else
            {
                // Default to DPAPI decryption for backward compatibility
                var provider = new DataProtectionProvider("LOCAL=user");
                IBuffer buffEncrypted = CryptographicBuffer.DecodeFromBase64String(config.EncryptedData);
                IBuffer buffDecrypted = await provider.UnprotectAsync(buffEncrypted);
                json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffDecrypted);
                Debug.WriteLine("[EncryptedSettings] Successfully decrypted using DPAPI");
            }

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

    /// <summary>
    /// Encrypts data using AES-GCM with a passphrase-derived key.
    /// Returns a JSON string containing salt, IV, tag, and encrypted data.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt</param>
    /// <param name="passphrase">The passphrase to use for encryption</param>
    /// <returns>Base64-encoded JSON object with encryption parameters</returns>
    private string EncryptWithPassphrase(string plainText, string passphrase)
    {
        // Generate random salt for key derivation
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Derive key from passphrase using PBKDF2
        using var keyDerivation = new Rfc2898DeriveBytes(
            passphrase,
            salt,
            iterations: 100000,
            HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32); // 256-bit key for AES-256

        // Generate random nonce (IV) for AES-GCM
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        // Prepare buffers
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        // Encrypt using AES-GCM
        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Package all components into a JSON structure
        var encryptedPackage = new
        {
            salt = Convert.ToBase64String(salt),
            nonce = Convert.ToBase64String(nonce),
            tag = Convert.ToBase64String(tag),
            data = Convert.ToBase64String(cipherBytes)
        };

        return JsonSerializer.Serialize(encryptedPackage);
    }

    /// <summary>
    /// Decrypts data that was encrypted with EncryptWithPassphrase.
    /// </summary>
    /// <param name="encryptedData">Base64-encoded JSON object with encryption parameters</param>
    /// <param name="passphrase">The passphrase to use for decryption</param>
    /// <returns>The decrypted plain text</returns>
    /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
    private string DecryptWithPassphrase(string encryptedData, string passphrase)
    {
        // Parse the encrypted package
        var package = JsonSerializer.Deserialize<Dictionary<string, string>>(encryptedData);
        if (package == null)
        {
            throw new CryptographicException("Invalid encrypted data format");
        }

        byte[] salt = Convert.FromBase64String(package["salt"]);
        byte[] nonce = Convert.FromBase64String(package["nonce"]);
        byte[] tag = Convert.FromBase64String(package["tag"]);
        byte[] cipherBytes = Convert.FromBase64String(package["data"]);

        // Derive the same key from passphrase
        using var keyDerivation = new Rfc2898DeriveBytes(
            passphrase,
            salt,
            iterations: 100000,
            HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32);

        // Decrypt using AES-GCM
        byte[] plainBytes = new byte[cipherBytes.Length];
        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}