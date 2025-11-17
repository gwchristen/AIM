using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Provides security services for authentication, authorization, and user access control.
/// Manages the master password, authorized users list, and security configuration persistence.
/// </summary>
public class SecurityService
{
    private readonly EncryptedSettingsService _encryptedSettingsService;
    private readonly ISettingsService _settingsService;
    private string _masterPassword;
    private List<string> _authorizedUsers = new();
    private bool _isMasterPasswordOverrideActive;
    
    /// <summary>
    /// Gets the current user ID based on the Windows account name.
    /// </summary>
    public string CurrentUserId { get; private set; }

    /// <summary>
    /// Gets whether the application is fully unlocked via master password override or authorized user status.
    /// </summary>
    public bool IsFullyUnlocked => _isMasterPasswordOverrideActive || IsCurrentUserAuthorized();

    /// <summary>
    /// Gets whether the master password override is currently active.
    /// </summary>
    public bool IsMasterPasswordOverrideActive => _isMasterPasswordOverrideActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityService"/> class.
    /// </summary>
    /// <param name="encryptedSettingsService">Service for managing encrypted security configuration.</param>
    /// <param name="settingsService">Service for managing application settings.</param>
    public SecurityService(EncryptedSettingsService encryptedSettingsService, ISettingsService settingsService)
    {
        _encryptedSettingsService = encryptedSettingsService;
        _settingsService = settingsService;
        CurrentUserId = Environment.UserName;

        // Load security config from storage
        InitializeSecurityAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Initializes security settings by loading the encrypted security configuration.
    /// If no configuration exists, sets up default settings with a default master password.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
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
    /// This includes the master password and list of authorized users.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="Exception">Thrown when the security configuration cannot be saved.</exception>
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
    /// Sets the master password for the application.
    /// This does not persist the password; call <see cref="SaveSecurityConfigAsync"/> to save changes.
    /// </summary>
    /// <param name="password">The new master password.</param>
    public void SetMasterPassword(string password)
    {
        _masterPassword = password;
    }

    /// <summary>
    /// Validates the provided password against the master password.
    /// If valid, activates the master password override for the current session.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns><c>true</c> if the password is correct; otherwise, <c>false</c>.</returns>
    public bool ValidateMasterPassword(string password)
    {
        _isMasterPasswordOverrideActive = password == _masterPassword;
        return _isMasterPasswordOverrideActive;
    }

    /// <summary>
    /// Changes the master password after validating the old password.
    /// The new password is persisted to encrypted storage automatically.
    /// </summary>
    /// <param name="oldPassword">The current master password.</param>
    /// <param name="newPassword">The new master password to set.</param>
    /// <returns><c>true</c> if the password was changed successfully; otherwise, <c>false</c>.</returns>
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
    /// Adds a user to the authorized users list.
    /// The list is automatically saved to encrypted storage.
    /// </summary>
    /// <param name="userId">The user ID to authorize (typically Windows username).</param>
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
    /// Removes a user from the authorized users list.
    /// The list is automatically saved to encrypted storage.
    /// </summary>
    /// <param name="userId">The user ID to remove from authorization.</param>
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
    /// <returns>A list containing all authorized user IDs.</returns>
    public List<string> GetAuthorizedUsers()
    {
        return _authorizedUsers.ToList();
    }

    /// <summary>
    /// Sets the complete authorized users list, replacing any existing users.
    /// The list is automatically saved to encrypted storage.
    /// </summary>
    /// <param name="users">The new list of authorized users, or null to clear the list.</param>
    public void SetAuthorizedUsers(List<string> users)
    {
        _authorizedUsers = users ?? new();
        SaveSecurityConfigAsync().ConfigureAwait(false);
        Debug.WriteLine($"[Security] Authorized users list updated - Count: {_authorizedUsers.Count}");
    }

    /// <summary>
    /// Checks whether the current Windows user is in the authorized users list.
    /// </summary>
    /// <returns><c>true</c> if the current user is authorized; otherwise, <c>false</c>.</returns>
    public bool IsCurrentUserAuthorized()
    {
        return _authorizedUsers.Any(u => u.Equals(CurrentUserId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Deactivates the master password override, requiring reauthorization.
    /// This does not affect users who are in the authorized users list.
    /// </summary>
    public void DeactivateMasterPasswordOverride()
    {
        _isMasterPasswordOverrideActive = false;
    }
}