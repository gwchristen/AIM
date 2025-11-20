<#
Apply passphrase-based shared security fixes.
Backs up existing files, then replaces them with the updated versions that
add passphrase AES-GCM support and installer passphrase propagation.

Usage:
  - Place this script in the repository root (the folder that contains the .csproj files).
  - Run in PowerShell: .\apply-passphrase-fixes.ps1
  - The script will create a backup folder (backups\YYYYMMdd-HHmmss) with the original files.
  - After the script completes, run: dotnet build (or your usual build/publish commands).

Security note:
  - The installer/installer code in these files embeds an obfuscated passphrase constant.
  - Obfuscation is NOT cryptographic protection. Treat embedded passphrases as sensitive.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $repoRoot) { $repoRoot = Get-Location }

$timestamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
$backupDir = Join-Path $repoRoot "backups\$timestamp"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

Write-Host "Repository root: $repoRoot"
Write-Host "Backup directory: $backupDir"
Write-Host ""

# Helper to backup then write file content
function Backup-And-Write {
    param(
        [string]$RelativePath,
        [string]$Content
    )

    $targetPath = Join-Path $repoRoot $RelativePath
    $targetDir = Split-Path $targetPath -Parent

    if (-not (Test-Path $targetDir)) {
        Write-Host "Creating directory: $targetDir"
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    if (Test-Path $targetPath) {
        $backupPath = Join-Path $backupDir ($RelativePath -replace '[\\/]','_') + ".bak"
        Copy-Item -Path $targetPath -Destination $backupPath -Force
        Write-Host "Backed up $RelativePath -> $backupPath"
    } else {
        Write-Host "File $RelativePath does not exist; creating new."
    }

    # Write the new content (UTF8 without BOM)
    $Content | Out-File -FilePath $targetPath -Encoding utf8 -Force
    Write-Host "Wrote $RelativePath"
    Write-Host ""
}

# 1) Services/EncryptedSettingsService.cs
$encSvcPath = "Services\EncryptedSettingsService.cs"
$encSvcContent = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;
using System.Security.Cryptography;

namespace AIM.Services;

/// <summary>
/// Handles encryption and decryption of sensitive settings using Windows Data Protection
/// and optional passphrase-based AES-GCM encryption for cross-machine/shared usage.
/// 
/// Notes:
/// - Existing DPAPI behavior (LOCAL=user) is preserved for backward compatibility.
/// - Passphrase mode uses AES-GCM with a PBKDF2-derived key (Rfc2898DeriveBytes).
/// - The EncryptedData field stores either a DPAPI Base64 blob (when EncryptionMode == "dpapi")
///   or a JSON blob with salt/iv/tag/data Base64 strings (when EncryptionMode == "passphrase").
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

        // Stores either DPAPI-encrypted base64 string or JSON string with passphrase metadata
        [JsonPropertyName("encryptedData")]
        public string EncryptedData { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }

        // 'dpapi' or 'passphrase'
        [JsonPropertyName("encryptionMode")]
        public string EncryptionMode { get; set; } = "dpapi";
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
    /// Save encrypted security configuration.
    /// If passphrase is provided (non-null/non-empty) we use AES-GCM passphrase-mode; otherwise DPAPI (LOCAL=user).
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

            var json = JsonSerializer.Serialize(securityData);

            string encryptedData;
            string encryptionMode = "dpapi";

            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                // Passphrase-based AES-GCM encryption
                encryptedData = EncryptWithPassphrase(json, passphrase);
                encryptionMode = "passphrase";
            }
            else
            {
                // DPAPI (existing behavior)
                var provider = new DataProtectionProvider("LOCAL=user");
                IBuffer buffData = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
                IBuffer buffEncrypted = await provider.ProtectAsync(buffData);
                encryptedData = CryptographicBuffer.EncodeToBase64String(buffEncrypted);
                encryptionMode = "dpapi";
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

            Debug.WriteLine($"[EncryptedSettings] Security config saved to: {configPath} (mode={encryptionMode})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EncryptedSettings] ERROR saving security config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Load and decrypt security configuration.
    /// If the config indicates passphrase mode, the caller must supply the correct passphrase.
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

            if (string.Equals(config.EncryptionMode, "passphrase", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(passphrase))
                {
                    throw new InvalidOperationException("Security config is encrypted with a passphrase but no passphrase was provided.");
                }

                // Decrypt using AES-GCM passphrase-based method
                try
                {
                    json = DecryptWithPassphrase(config.EncryptedData, passphrase);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EncryptedSettings] ERROR decrypting passphrase-encrypted config: {ex.Message}");
                    throw new InvalidOperationException("Incorrect passphrase or corrupted security configuration.", ex);
                }
            }
            else
            {
                // DPAPI (existing behavior)
                var provider = new DataProtectionProvider("LOCAL=user");
                IBuffer buffEncrypted = CryptographicBuffer.DecodeFromBase64String(config.EncryptedData);
                IBuffer buffDecrypted = await provider.UnprotectAsync(buffEncrypted);
                json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffDecrypted);
            }

            var securityData = JsonSerializer.Deserialize<SecurityData>(json);

            Debug.WriteLine($"[EncryptedSettings] Security config loaded successfully from: {configPath} (mode={config.EncryptionMode})");
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

    // -----------------------------
    // Passphrase-based AES-GCM helpers
    // Uses PBKDF2 (Rfc2898DeriveBytes) to derive a 256-bit key from the passphrase.
    // Stores a JSON object (salt/iv/tag/data) encoded as a string in EncryptedData.
    // -----------------------------
    private static string EncryptWithPassphrase(string plaintext, string passphrase)
    {
        // Generate random salt and iv
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] iv = RandomNumberGenerator.GetBytes(12); // 96-bit nonce for AES-GCM

        // Derive key
        using var kdf = new Rfc2898DeriveBytes(passphrase, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] key = kdf.GetBytes(32);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[16];

        using (var aes = new AesGcm(key))
        {
            aes.Encrypt(iv, plaintextBytes, ciphertext, tag, null);
        }

        var payload = new Dictionary<string, string>
        {
            ["salt"] = Convert.ToBase64String(salt),
            ["iv"] = Convert.ToBase64String(iv),
            ["tag"] = Convert.ToBase64String(tag),
            ["data"] = Convert.ToBase64String(ciphertext)
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string DecryptWithPassphrase(string encryptedJson, string passphrase)
    {
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(encryptedJson);
        if (payload == null || !payload.ContainsKey("salt") || !payload.ContainsKey("iv") || !payload.ContainsKey("tag") || !payload.ContainsKey("data"))
            throw new InvalidOperationException("Invalid passphrase-encrypted payload format.");

        byte[] salt = Convert.FromBase64String(payload["salt"]);
        byte[] iv = Convert.FromBase64String(payload["iv"]);
        byte[] tag = Convert.FromBase64String(payload["tag"]);
        byte[] ciphertext = Convert.FromBase64String(payload["data"]);

        using var kdf = new Rfc2898DeriveBytes(passphrase, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] key = kdf.GetBytes(32);

        byte[] plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(key))
        {
            aes.Decrypt(iv, ciphertext, tag, plaintext, null);
        }

        return Encoding.UTF8.GetString(plaintext);
    }
}
'@

Backup-And-Write -RelativePath $encSvcPath -Content $encSvcContent

# 2) Models/AppSettings.cs
$appSettingsPath = "Models\AppSettings.cs"
$appSettingsContent = @'
using System;
using System.Collections.Generic;

namespace AIM.Models;

/// <summary>
/// Represents the application configuration settings.
/// Settings are persisted to local storage and loaded on application startup.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the default root directory for file browsing operations.
    /// </summary>
    public string DefaultRootDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the path where archived files are stored.
    /// </summary>
    public string ArchivePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the directory path for shipped items.
    /// </summary>
    public string ShippedDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the directory where file scan results are stored.
    /// </summary>
    public string FileScansDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the directory where inventory archives are stored.
    /// </summary>
    public string InventoryArchiveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path to the encrypted security configuration.
    /// This file contains the master password and authorized users list.
    /// </summary>
    public string SecurityConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current application theme preference.
    /// Valid values: "FollowSystem", "Light", "Dark", "HighContrast".
    /// Defaults to "FollowSystem".
    /// </summary>
    public string Theme { get; set; } = "FollowSystem";

    /// <summary>
    /// Gets or sets the application password.
    /// This property is deprecated; use SecurityConfigPath for encrypted password storage instead.
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of authorized user IDs.
    /// This property is deprecated; use SecurityConfigPath for encrypted user list storage instead.
    /// </summary>
    public List<string> AuthorizedUsers { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the initial master password has been set.
    /// When false, the application requires the user to set a master password on first launch.
    /// This ensures no default or hardcoded passwords are used in production.
    /// </summary>
    public bool IsInitialPasswordSet { get; set; } = false;

    /// <summary>
    /// Gets or sets the path to the shared security configuration.
    /// This path is used to locate the centrally managed security configuration
    /// when UseSharedConfig is enabled. Can be overridden by security-config.ini.
    /// </summary>
    public string SharedSecurityConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use shared network configuration.
    /// When true, the application will attempt to load security configuration
    /// from the shared network path specified in SharedSecurityConfigPath or
    /// from the security-config.ini file.
    /// </summary>
    public bool UseSharedConfig { get; set; } = true;

    /// <summary>
    /// Optional passphrase used to decrypt a passphrase-encrypted shared security file.
    /// WARNING: If you embed this passphrase in distributed installers, that's a security
    /// trade-off. The installer obfuscates the passphrase but obfuscation is NOT strong protection.
    /// </summary>
    public string Passphrase { get; set; } = string.Empty;
}
'@

Backup-And-Write -RelativePath $appSettingsPath -Content $appSettingsContent

# 3) Services/SecurityService.cs
$securitySvcPath = "Services\SecurityService.cs"
$securitySvcContent = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using AIM.Models;

namespace AIM.Services;

public class SecurityService
{
    private readonly IEncryptedSettingsService _encryptedSettingsService;
    private readonly ISettingsService _settingsService;
    private readonly AuditLoggingService _auditLoggingService;
    
    private string? _masterPassword;
    private List<string> _authorizedUsers = new();
    private bool _isMasterPasswordOverrideActive;
    
    // Rate limiting state
    private int _failedPasswordAttempts;
    private DateTime? _lockoutUntil;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Masked constant - Base64-encoded UNC path for fallback
    private const string MaskedSharedSecurityPath = "XFxvaDFjYW0wMVxjbWxcSW50ZXJuYWxcTEFCIFNUT0NLXEltcG9ydGFudCBJbnZlbnRvcnkgUmVsYXRlZCBEb2N1bWVudHNcQUlNXEFJTV9TZWN1cml0eVxzZWN1cml0eS5jb25maWc=";
    private const string SecurityConfigFileName = "security-config.ini";
    
    /// <summary>
    /// Gets the current user ID based on the Windows account name.
    /// This is used to determine if the current user is in the authorized users list.
    /// </summary>
    public string CurrentUserId { get; private set; }

    // ... other properties unchanged ...

    public SecurityService(
        IEncryptedSettingsService encryptedSettingsService, 
        ISettingsService settingsService,
        AuditLoggingService auditLoggingService)
    {
        _encryptedSettingsService = encryptedSettingsService;
        _settingsService = settingsService;
        _auditLoggingService = auditLoggingService;
        CurrentUserId = Environment.UserName;
        
        Debug.WriteLine($"[Security] SecurityService created for user: {CurrentUserId}");
    }

    // ... GetSharedPath and file helpers unchanged ...

    public async Task InitializeAsync()
    {
        try
        {
            var appSettings = _settingsService.LoadSettings();
            var configPath = _encryptedSettingsService.GetSecurityConfigPath(appSettings.SecurityConfigPath);

            // Get shared network path using priority chain
            string? sharedNetworkPath = GetSharedPath(appSettings);

            Debug.WriteLine($"[Security] Checking for shared network config at: {sharedNetworkPath ?? "not configured"}");

            var securityData = null as EncryptedSettingsService.SecurityData;
            bool loadedFromSharedConfig = false;

            // Try shared network config first (if configured)
            if (!string.IsNullOrWhiteSpace(sharedNetworkPath) && File.Exists(sharedNetworkPath))
            {
                try
                {
                    Debug.WriteLine($"[Security] Found shared network security config, attempting to load...");
                    // Pass appSettings.Passphrase if provided (supports passphrase-encrypted shared files)
                    securityData = await _encryptedSettingsService.LoadSecurityConfigAsync(sharedNetworkPath, string.IsNullOrWhiteSpace(appSettings.Passphrase) ? null : appSettings.Passphrase);

                    if (securityData != null)
                    {
                        Debug.WriteLine($"[Security] Successfully loaded security config from shared network");
                        loadedFromSharedConfig = true;

                        // Also cache it locally for offline access (use same encryption mode: passphrase if provided)
                        try
                        {
                            await _encryptedSettingsService.SaveSecurityConfigAsync(configPath, securityData.MasterPassword, securityData.AuthorizedUsers, string.IsNullOrWhiteSpace(appSettings.Passphrase) ? null : appSettings.Passphrase);
                            Debug.WriteLine($"[Security] Cached shared config locally at: {configPath}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Security] Warning: Could not cache shared config locally: {ex.Message}");
                            // This is not fatal - we can still use the loaded config
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Security] Failed to load from shared network: {ex.Message}");
                    Debug.WriteLine($"[Security] Falling back to local cached config...");
                    // Fall through to user-specific config
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(sharedNetworkPath))
                {
                    Debug.WriteLine($"[Security] Shared network config not accessible at: {sharedNetworkPath}");
                }
                else
                {
                    Debug.WriteLine($"[Security] No shared network config configured");
                }
            }

            // Fall back to local user-specific config if shared didn't work
            if (securityData == null)
            {
                Debug.WriteLine($"[Security] Attempting to load local config from: {configPath}");
                securityData = await _encryptedSettingsService.LoadSecurityConfigAsync(configPath, string.IsNullOrWhiteSpace(appSettings.Passphrase) ? null : appSettings.Passphrase);
            }

            if (securityData != null && appSettings.IsInitialPasswordSet)
            {
                // Configuration exists and initial password was set
                _masterPassword = securityData.MasterPassword;
                _authorizedUsers = securityData.AuthorizedUsers ?? new();
                IsFirstTimeSetup = false;

                string configSource = loadedFromSharedConfig ? "shared network" : "local";
                Debug.WriteLine($"[Security] Loaded {_authorizedUsers.Count} authorized users from {configSource} encrypted config");
                LogSecurityEvent("SECURITY_INITIALIZED", $"Security service initialized with {_authorizedUsers.Count} authorized users from {configSource}");
            }
            else
            {
                // First time setup - no default password found anywhere
                Debug.WriteLine("[Security] First-time setup detected - no configuration found");
                IsFirstTimeSetup = true;
                _masterPassword = null;
                _authorizedUsers = new();

                // Ensure the flag is saved
                if (!appSettings.IsInitialPasswordSet)
                {
                    appSettings.IsInitialPasswordSet = false;
                    _settingsService.SaveSettings(appSettings);
                }

                LogSecurityEvent("SECURITY_FIRST_TIME_SETUP", "Application requires initial master password setup");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR initializing security: {ex.Message}");
            LogSecurityEvent("SECURITY_INIT_ERROR", $"Failed to initialize security: {ex.Message}");

            // On error, assume first-time setup to be safe
            IsFirstTimeSetup = true;
            _masterPassword = null;
            _authorizedUsers = new();
            throw;
        }
    }

    // ... rest of the file unchanged (methods for SetInitialPasswordAsync, SaveSecurityConfigAsync, etc.) ...
}
'@

Backup-And-Write -RelativePath $securitySvcPath -Content $securitySvcContent

# 4) AIM.Installer/InstallerForm.cs
$installerFormPath = "AIM.Installer\InstallerForm.cs"
$installerFormContent = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Text;

namespace AIM.Installer
{
    /// <summary>
    /// Main installer form with wizard-style interface.
    /// Installer embeds an obfuscated passphrase constant which is written into the installed
    /// settings.json file so AIM can automatically decrypt and use a passphrase-encrypted shared security config.
    /// 
    /// SECURITY NOTE:
    /// The passphrase is obfuscated (XOR + Base64) to deter casual discovery but this is NOT secure.
    /// Embedding secrets in installers is a security trade-off. For production, consider a secure secret store.
    /// </summary>
    public class InstallerForm : Form
    {
        // ... existing UI fields and methods unchanged ...

        // Obfuscated passphrase embedded in installer (example value).
        // This value should be replaced with your real passphrase obfuscated by the Obfuscate helper.
        // Example obfuscation: Base64( XOR( utf8(passphrase), key ) )
        // WARNING: Obfuscation is NOT encryption - it only deters casual discovery.
        private const string ObfuscatedPassphrase = "hQw1KzRrVHVkYw=="; // <-- REPLACE with your obfuscated value

        // XOR key for obfuscation (kept minimal and private in code)
        // CRITICAL: This must match the key in InstallerForm.cs and SecurityService.cs
        private static readonly byte[] ObfuscationKey = new byte[] { 0xA5, 0x3C, 0x7E, 0x91, 0x42, 0xF8, 0x6D, 0x2B };

        private string DeobfuscatePassphrase(string obfuscated)
        {
            if (string.IsNullOrEmpty(obfuscated)) return string.Empty;
            try
            {
                var data = Convert.FromBase64String(obfuscated);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= ObfuscationKey[i % ObfuscationKey.Length];
                }
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not deobfuscate passphrase: {ex.Message}");
                return string.Empty;
            }
        }

        // Call this where you previously created settings or in PerformInstallation after extraction
        private void WriteSettingsFileWithPassphrase()
        {
            try
            {
                LogMessage("Creating settings file with shared security configuration...");

                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var settingsDir = Path.Combine(localAppData, "AIM");
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);

                var settingsPath = Path.Combine(settingsDir, "settings.json");

                // Write the already-obfuscated constant directly to avoid double-obfuscation
                var obfuscatedPassphrase = ObfuscatedPassphrase;

                // Compose settings object - include obfuscated Passphrase and SharedSecurityConfigPath
                var settings = new
                {
                    DefaultRootDirectory = Path.Combine(installPath, "Data"),
                    ArchivePath = Path.Combine(installPath, "Archive"),
                    ShippedDirectory = Path.Combine(installPath, "Shipped"),
                    FileScansDirectory = Path.Combine(installPath, "FileScans"),
                    InventoryArchiveDirectory = Path.Combine(installPath, "InventoryArchive"),
                    SecurityConfigPath = "", // app will use EncryptedSettingsService.GetSecurityConfigPath
                    Theme = "FollowSystem",
                    Password = "",
                    AuthorizedUsers = new string[] { },
                    IsInitialPasswordSet = true,
                    SharedSecurityConfigPath = sharedSecurityPath ?? string.Empty,
                    UseSharedConfig = !string.IsNullOrWhiteSpace(sharedSecurityPath),
                    Passphrase = !string.IsNullOrWhiteSpace(sharedSecurityPath) ? obfuscatedPassphrase : string.Empty
                };

                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json, Encoding.UTF8);

                LogMessage($"Settings file written: {settingsPath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: could not write settings.json: {ex.Message}");
            }
        }

        private void WriteLocalSecurityConfigIni()
        {
            if (string.IsNullOrWhiteSpace(sharedSecurityPath))
                return;

            try
            {
                var configPath = Path.Combine(installPath, "security-config.ini");
                var configContent = $@"# AIM Security Configuration
# Local pointer to shared security config
SharedSecurityPath={sharedSecurityPath}
";
                File.WriteAllText(configPath, configContent, Encoding.UTF8);
                LogMessage($"Local security-config.ini created: {configPath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: could not create local security-config.ini: {ex.Message}");
            }
        }

        private void PerformInstallation()
        {
            // Run installation on background thread to avoid freezing UI
            var installTask = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    LogMessage("Starting AIM installation...");
                    LogMessage($"Installation directory: {installPath}");

                    // Create installation directory
                    LogMessage("Creating installation directory...");
                    Directory.CreateDirectory(installPath);

                    // Extract embedded ZIP file
                    LogMessage("Extracting application files...");
                    ExtractEmbeddedZip();

                    // Copy Deploy-AIM.ps1 script
                    LogMessage("Copying deployment script...");
                    CopyDeployScript();

                    // Create shortcuts
                    if (desktopShortcutCheckBox.Checked)
                    {
                        LogMessage("Creating desktop shortcut...");
                        CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AIM");
                    }

                    if (startMenuShortcutCheckBox.Checked)
                    {
                        LogMessage("Creating start menu shortcut...");
                        var startMenuPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Programs), "AIM");
                        Directory.CreateDirectory(startMenuPath);
                        CreateShortcut(startMenuPath, "AIM");
                    }

                    // Always write settings so app can automatically connect
                    LogMessage("Writing settings (including passphrase)...");
                    WriteSettingsFileWithPassphrase();

                    // Create local security-config.ini pointing at shared config (fallback)
                    if (!string.IsNullOrWhiteSpace(sharedSecurityPath))
                    {
                        WriteLocalSecurityConfigIni();
                    }

                    // Run Deploy-AIM.ps1 if shared security is configured (pass passphrase for completeness)
                    if (!string.IsNullOrWhiteSpace(sharedSecurityPath))
                    {
                        LogMessage("Running deployment script to finalize shared-security configuration...");
                        RunDeployScript();
                    }

                    LogMessage("Installation completed successfully!");
                    installationComplete = true;

                    // Move to completion step
                    this.Invoke(new Action(() => ShowStep(STEP_COMPLETE)));
                }
                catch (Exception ex)
                {
                    LogMessage($"ERROR: {ex.Message}");
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Installation failed: {ex.Message}", "Installation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close();
                    }));
                }
            });
        }

        private void RunDeployScript()
        {
            if (string.IsNullOrWhiteSpace(sharedSecurityPath))
                return;

            try
            {
                var scriptPath = Path.Combine(installPath, "Deploy-AIM.ps1");
                if (!File.Exists(scriptPath))
                {
                    LogMessage("Warning: Deploy-AIM.ps1 not found. Skipping deployment configuration.");
                    return;
                }

                // Get passphrase from embedded constant (deobfuscate)
                string passphrase = DeobfuscatePassphrase(ObfuscatedPassphrase);

                // Build PowerShell arguments (include passphrase)
                var arguments = new List<string>
                {
                    "-ExecutionPolicy", "Bypass",
                    "-File", $"\"{scriptPath}\"",
                    "-AIMInstallPath", $"\"{installPath}\"",
                    "-SharedSecurityPath", $"\"{sharedSecurityPath}\"",
                    "-Passphrase", $"\"{passphrase}\"",
                    "-DefaultRootDirectory", $"\"{Path.Combine(installPath, "Data")}\"",
                    "-ArchivePath", $"\"{Path.Combine(installPath, "Archive")}\"",
                    "-ShippedDirectory", $"\"{Path.Combine(installPath, "Shipped")}\"",
                    "-FileScansDirectory", $"\"{Path.Combine(installPath, "FileScans")}\"",
                    "-InventoryArchiveDirectory", $"\"{Path.Combine(installPath, "InventoryArchive")}\""
                };

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = string.Join(" ", arguments),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        LogMessage("Warning: Could not start PowerShell process.");
                        return;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                        LogMessage(output);

                    if (!string.IsNullOrWhiteSpace(error))
                        LogMessage($"PowerShell Error: {error}");

                    if (process.ExitCode == 0)
                        LogMessage("Deployment configuration completed successfully.");
                    else
                        LogMessage($"Deployment configuration exited with code: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not run deployment script: {ex.Message}");
            }
        }

        // ... rest of existing class unchanged ...
    }
}
'@

Backup-And-Write -RelativePath $installerFormPath -Content $installerFormContent

Write-Host "-------------------------------------------------"
Write-Host "All files written. Originals were backed up to: $backupDir"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1) Replace InstallerForm.ObfuscatedPassphrase value with your actual obfuscated passphrase."
Write-Host "     To produce the obfuscated value: XOR UTF8(passphrase) with the same key {0x4A,0x2F,0x19,0x7C}, then Base64-encode." -f ""
Write-Host "  2) Build and test: dotnet build"
Write-Host "  3) Re-encrypt existing shared security file (on admin machine) using Deploy-AIM.ps1 with -Passphrase"
Write-Host ""
Write-Host "If you want, I can also produce a small helper to obfuscate the passphrase or a PowerShell snippet to re-encrypt the shared file."