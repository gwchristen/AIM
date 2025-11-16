using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class SecurityService
{
    private readonly EncryptedSettingsService _encryptedSettingsService;
    private readonly ISettingsService _settingsService;
    private string _masterPassword;
    private List<string> _authorizedUsers = new();
    private bool _isMasterPasswordOverrideActive;
    public string CurrentUserId { get; private set; }

    public bool IsFullyUnlocked => _isMasterPasswordOverrideActive || IsCurrentUserAuthorized();

    public bool IsMasterPasswordOverrideActive => _isMasterPasswordOverrideActive;

    public SecurityService(EncryptedSettingsService encryptedSettingsService, ISettingsService settingsService)
    {
        _encryptedSettingsService = encryptedSettingsService;
        _settingsService = settingsService;
        CurrentUserId = Environment.UserName;

        // Load security config from storage
        InitializeSecurityAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Initialize security by loading encrypted config
    /// </summary>
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
    /// Save security config to encrypted storage
    /// </summary>
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

    public void SetMasterPassword(string password)
    {
        _masterPassword = password;
    }

    public bool ValidateMasterPassword(string password)
    {
        _isMasterPasswordOverrideActive = password == _masterPassword;
        return _isMasterPasswordOverrideActive;
    }

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

    public void AddAuthorizedUser(string userId)
    {
        if (!_authorizedUsers.Contains(userId))
        {
            _authorizedUsers.Add(userId);
            SaveSecurityConfigAsync().ConfigureAwait(false);
            Debug.WriteLine($"[Security] Added authorized user: {userId}");
        }
    }

    public void RemoveAuthorizedUser(string userId)
    {
        if (_authorizedUsers.Remove(userId))
        {
            SaveSecurityConfigAsync().ConfigureAwait(false);
            Debug.WriteLine($"[Security] Removed authorized user: {userId}");
        }
    }

    public List<string> GetAuthorizedUsers()
    {
        return _authorizedUsers.ToList();
    }

    /// <summary>
    /// Set the authorized users list (used for loading from settings)
    /// </summary>
    public void SetAuthorizedUsers(List<string> users)
    {
        _authorizedUsers = users ?? new();
        SaveSecurityConfigAsync().ConfigureAwait(false);
        Debug.WriteLine($"[Security] Authorized users list updated - Count: {_authorizedUsers.Count}");
    }

    public bool IsCurrentUserAuthorized()
    {
        return _authorizedUsers.Any(u => u.Equals(CurrentUserId, StringComparison.OrdinalIgnoreCase));
    }

    public void DeactivateMasterPasswordOverride()
    {
        _isMasterPasswordOverrideActive = false;
    }
}