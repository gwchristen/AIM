using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Manages application security including authentication, authorization, and master password override functionality.
/// 
/// This service uses a hybrid authentication model combining:
/// 1. Master password override for administrative access
/// 2. Authorized users list for role-based access control
/// 
/// <example>
/// <code>
/// // Check if user is fully unlocked
/// if (_securityService.IsFullyUnlocked)
/// {
///     // Allow access to restricted features
/// }
/// 
/// // Validate master password
/// if (_securityService.ValidateMasterPassword("YourPassword"))
/// {
///     // User has override access
/// }
/// 
/// // Add authorized user
/// _securityService.AddAuthorizedUser("domain\\username");
/// </code>
/// </example>
/// </summary>
public class SecurityService
{
    private readonly EncryptedSettingsService _encryptedSettingsService;
    private readonly ISettingsService _settingsService;
    private string _masterPassword;
    private List<string> _authorizedUsers = new();
    private bool _isMasterPasswordOverrideActive;

    /// <summary>
    /// Gets the current Windows username.
    /// </summary>
    public string CurrentUserId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current session is fully unlocked.
    /// Returns true if either master password override is active OR the current user is in the authorized users list.
    /// </summary>
    public bool IsFullyUnlocked => _isMasterPasswordOverrideActive || IsCurrentUserAuthorized();

    /// <summary>
    /// Gets a value indicating whether master password override is currently active.
    /// </summary>
    public bool IsMasterPasswordOverrideActive => _isMasterPasswordOverrideActive;

    /// <summary>
    /// Initializes a new instance of the SecurityService class.
    /// </summary>
    /// <param name="encryptedSettingsService">Service for managing encrypted security configuration</param>
    /// <param name="settingsService">Service for managing application settings</param>
    /// <remarks>
    /// The constructor automatically initializes security by loading encrypted configuration.
    /// If no configuration exists, it creates a default one with the default master password.
    /// </remarks>
    public SecurityService(EncryptedSettingsService encryptedSettingsService, ISettingsService settingsService)
    {
        _encryptedSettingsService = encryptedSettingsService;
        _settingsService = settingsService;
        CurrentUserId = Environment.UserName;

        // Load security config from storage
        InitializeSecurityAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Initializes security by loading encrypted configuration from storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous but called without await in the constructor for performance.
    /// It loads the master password and authorized users list from encrypted storage.
    /// If no configuration exists on first launch, default values are used.
    /// </remarks>
    private async Task InitializeSecurityAsync()
    {
        try
        {
            var appSettings = _settingsService.LoadSettings();
            var configPath = _encryptedSettingsService.GetSecurityConfigPath(appSettings.SecurityConfigPath);

            var securityData = await _encryptedSettingsService.LoadSecurityConfigAsync(configPath);

            if (securityData != null)
            {
                _masterPassword = securityData.MasterPassword;
                _authorizedUsers = securityData.AuthorizedUsers ?? new();

                Debug.WriteLine($"[Security] Loaded {_authorizedUsers.Count} authorized users from encrypted config");
            }
            else
            {
                // First time setup - use default master password
                _masterPassword = "AIMAdmin123";
                _authorizedUsers = new();
                await SaveSecurityConfigAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR initializing security: {ex.Message}");
            _masterPassword = "AIMAdmin123";
            _authorizedUsers = new();
        }
    }

    /// <summary>
    /// Saves the current security configuration to encrypted storage.
    /// </summary>
    /// <remarks>
    /// This method encrypts the master password and authorized users list using AES-256 encryption
    /// before writing to the security configuration file.
    /// </remarks>
    /// <exception cref="Exception">Thrown if the configuration cannot be saved to storage</exception>
    public async Task SaveSecurityConfigAsync()
    {
        try
        {
            var appSettings = _settingsService.LoadSettings();
            var configPath = _encryptedSettingsService.GetSecurityConfigPath(appSettings.SecurityConfigPath);

            await _encryptedSettingsService.SaveSecurityConfigAsync(configPath, _masterPassword, _authorizedUsers);

            Debug.WriteLine($"[Security] Security config saved to: {configPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR saving security config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets the master password for administrative override.
    /// </summary>
    /// <param name="password">The new master password</param>
    /// <remarks>
    /// This method only updates the in-memory password. Call SaveSecurityConfigAsync() to persist changes.
    /// ⚠️ WARNING: This should only be called after strong password validation.
    /// </remarks>
    public void SetMasterPassword(string password)
    {
        _masterPassword = password;
    }

    /// <summary>
    /// Validates the provided master password and activates override if correct.
    /// </summary>
    /// <param name="password">The master password to validate</param>
    /// <returns>True if the password is correct and override is activated; otherwise false</returns>
    /// <remarks>
    /// Setting the master password activates the override immediately.
    /// Call DeactivateMasterPasswordOverride() to disable it.
    /// </remarks>
    public bool ValidateMasterPassword(string password)
    {
        _isMasterPasswordOverrideActive = password == _masterPassword;
        return _isMasterPasswordOverrideActive;
    }

    /// <summary>
    /// Changes the master password after validating the current one.
    /// </summary>
    /// <param name="oldPassword">The current master password for validation</param>
    /// <param name="newPassword">The new master password to set</param>
    /// <returns>True if the old password is correct and change was successful; otherwise false</returns>
    /// <remarks>
    /// This method automatically saves the new password to encrypted storage.
    /// If the old password is incorrect, no changes are made.
    /// </remarks>
    public bool ChangeMasterPassword(string oldPassword, string newPassword)
    {
        if (oldPassword != _masterPassword)
        {
            return false;
        }

        _masterPassword = newPassword;
        SaveSecurityConfigAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Adds a Windows username to the authorized users list.
    /// </summary>
    /// <param name="userId">The Windows username to authorize (format: "domain\username" or "username")</param>
    /// <remarks>
    /// The user is only added if not already present. Changes are automatically saved to encrypted storage.
    /// Example usernames: "CONTOSO\jdoe" or "jdoe"
    /// </remarks>
    public void AddAuthorizedUser(string userId)
    {
        if (!_authorizedUsers.Contains(userId))
        {
            _authorizedUsers.Add(userId);
            SaveSecurityConfigAsync().ConfigureAwait(false);
            Debug.WriteLine($"[Security] Added authorized user: {userId}");
        }
    }

    /// <summary>
    /// Removes a Windows username from the authorized users list.
    /// </summary>
    /// <param name="userId">The Windows username to remove</param>
    /// <remarks>
    /// If the user exists in the list, they are removed and changes are automatically saved.
    /// If the user is not found, no action is taken.
    /// </remarks>
    public void RemoveAuthorizedUser(string userId)
    {
        if (_authorizedUsers.Remove(userId))
        {
            SaveSecurityConfigAsync().ConfigureAwait(false);
            Debug.WriteLine($"[Security] Removed authorized user: {userId}");
        }
    }

    /// <summary>
    /// Gets a copy of the authorized users list.
    /// </summary>
    /// <returns>A list of authorized Windows usernames</returns>
    /// <remarks>
    /// Returns a copy to prevent external modification of the internal list.
    /// </remarks>
    public List<string> GetAuthorizedUsers()
    {
        return _authorizedUsers.ToList();
    }

    /// <summary>
    /// Sets the complete authorized users list, replacing the current one.
    /// </summary>
    /// <param name="users">The new list of authorized usernames</param>
    /// <remarks>
    /// This method replaces the entire authorized users list and saves it to encrypted storage.
    /// Useful for loading users from application settings during initialization.
    /// </remarks>
    public void SetAuthorizedUsers(List<string> users)
    {
        _authorizedUsers = users ?? new();
        SaveSecurityConfigAsync().ConfigureAwait(false);
        Debug.WriteLine($"[Security] Authorized users list updated - Count: {_authorizedUsers.Count}");
    }

    /// <summary>
    /// Checks if the current user is in the authorized users list.
    /// </summary>
    /// <returns>True if the current user is authorized; otherwise false</returns>
    /// <remarks>
    /// Comparison is case-insensitive to handle different username formats.
    /// </remarks>
    public bool IsCurrentUserAuthorized()
    {
        return _authorizedUsers.Any(u => u.Equals(CurrentUserId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Deactivates the master password override.
    /// </summary>
    /// <remarks>
    /// After calling this method, the user will need to re-authenticate using their authorized status
    /// or provide the master password again to access restricted features.
    /// </remarks>
    public void DeactivateMasterPasswordOverride()
    {
        _isMasterPasswordOverrideActive = false;
    }
}