using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Provides comprehensive security services for authentication, authorization, and user access control.
/// 
/// <para><strong>Security Architecture:</strong></para>
/// <para>
/// This service implements a two-tier authentication model:
/// 1. <strong>Master Password:</strong> A single administrative password that grants full access to all features.
///    The master password can be used to override standard authorization checks temporarily.
/// 2. <strong>Authorized Users:</strong> A list of Windows usernames that are permanently authorized to access
///    restricted features without needing the master password.
/// </para>
/// 
/// <para><strong>Authentication Flow:</strong></para>
/// <list type="number">
/// <item>On first launch, the user must set a strong master password (no default password is used).</item>
/// <item>Users are identified by their Windows username (Environment.UserName).</item>
/// <item>Access is granted if either:
///   <list type="bullet">
///     <item>The current Windows user is in the authorized users list, OR</item>
///     <item>The master password has been entered and validated (master password override active)</item>
///   </list>
/// </item>
/// <item>The master password override can be deactivated at any time, reverting to user-based authorization.</item>
/// </list>
/// 
/// <para><strong>Encryption and Key Derivation:</strong></para>
/// <para>
/// The master password and authorized users list are stored in an encrypted configuration file using
/// Windows Data Protection API (DPAPI). The encryption is machine and user specific, meaning:
/// - Encrypted data cannot be decrypted on a different machine
/// - Encrypted data cannot be decrypted by a different Windows user
/// - No encryption keys are stored in code or configuration files
/// </para>
/// 
/// <para><strong>Security Features:</strong></para>
/// <list type="bullet">
/// <item>Strong password enforcement (8+ chars, uppercase, lowercase, numbers, symbols)</item>
/// <item>Rate limiting: 5 failed password attempts trigger a 15-minute lockout</item>
/// <item>All authentication attempts are logged to the audit log</item>
/// <item>No hardcoded or default passwords</item>
/// <item>First-time setup flow requires initial password configuration</item>
/// </list>
/// 
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// This service is designed to be used as a singleton. Rate limiting state is stored in-memory
/// and is not persisted across application restarts.
/// </para>
/// </summary>
public class SecurityService
{
    private readonly EncryptedSettingsService _encryptedSettingsService;
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
    
    /// <summary>
    /// Gets the current user ID based on the Windows account name.
    /// This is used to determine if the current user is in the authorized users list.
    /// </summary>
    public string CurrentUserId { get; private set; }

    /// <summary>
    /// Gets whether the application is fully unlocked via master password override or authorized user status.
    /// When true, the user has access to all restricted features in the application.
    /// </summary>
    public bool IsFullyUnlocked => _isMasterPasswordOverrideActive || IsCurrentUserAuthorized();

    /// <summary>
    /// Gets whether the master password override is currently active.
    /// The override remains active until explicitly deactivated or the application is restarted.
    /// </summary>
    public bool IsMasterPasswordOverrideActive => _isMasterPasswordOverrideActive;

    /// <summary>
    /// Gets whether the application is in first-time setup mode (no master password configured yet).
    /// When true, the application should prompt the user to set an initial master password.
    /// </summary>
    public bool IsFirstTimeSetup { get; private set; }

    /// <summary>
    /// Gets whether authentication is currently locked out due to too many failed attempts.
    /// Lockout is automatically cleared after the lockout duration expires.
    /// </summary>
    public bool IsLockedOut
    {
        get
        {
            if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
            {
                return true;
            }
            
            // Lockout period expired, clear it
            if (_lockoutUntil.HasValue && DateTime.UtcNow >= _lockoutUntil.Value)
            {
                _lockoutUntil = null;
                _failedPasswordAttempts = 0;
                Debug.WriteLine("[Security] Lockout period expired, authentication re-enabled");
            }
            
            return false;
        }
    }

    /// <summary>
    /// Gets the remaining lockout time if currently locked out.
    /// Returns null if not locked out.
    /// </summary>
    public TimeSpan? RemainingLockoutTime
    {
        get
        {
            if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
            {
                return _lockoutUntil.Value - DateTime.UtcNow;
            }
            return null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityService"/> class.
    /// Note: This constructor does NOT perform async initialization. Call <see cref="InitializeAsync"/>
    /// after construction to load the security configuration.
    /// </summary>
    /// <param name="encryptedSettingsService">Service for managing encrypted security configuration.</param>
    /// <param name="settingsService">Service for managing application settings.</param>
    /// <param name="auditLoggingService">Service for logging security events.</param>
    public SecurityService(
        EncryptedSettingsService encryptedSettingsService, 
        ISettingsService settingsService,
        AuditLoggingService auditLoggingService)
    {
        _encryptedSettingsService = encryptedSettingsService;
        _settingsService = settingsService;
        _auditLoggingService = auditLoggingService;
        CurrentUserId = Environment.UserName;
        
        Debug.WriteLine($"[Security] SecurityService created for user: {CurrentUserId}");
    }

    /// <summary>
    /// Asynchronously initializes the security service by loading the encrypted security configuration.
    /// This method MUST be called after construction and before using any security features.
    /// 
    /// <para><strong>Initialization Flow:</strong></para>
    /// <list type="number">
    /// <item>Checks if a security configuration file exists.</item>
    /// <item>If exists: Loads master password and authorized users from encrypted storage.</item>
    /// <item>If not exists: Sets IsFirstTimeSetup flag to require initial password configuration.</item>
    /// <item>Updates AppSettings to reflect first-time setup status.</item>
    /// </list>
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <exception cref="Exception">Thrown when the security configuration cannot be loaded.</exception>
    public async Task InitializeAsync()
    {
        try
        {
            var appSettings = _settingsService.LoadSettings();
            var configPath = _encryptedSettingsService.GetSecurityConfigPath(appSettings.SecurityConfigPath);

            var securityData = await _encryptedSettingsService.LoadSecurityConfigAsync(configPath);

            if (securityData != null && appSettings.IsInitialPasswordSet)
            {
                // Configuration exists and initial password was set
                _masterPassword = securityData.MasterPassword;
                _authorizedUsers = securityData.AuthorizedUsers ?? new();
                IsFirstTimeSetup = false;

                Debug.WriteLine($"[Security] Loaded {_authorizedUsers.Count} authorized users from encrypted config");
                LogSecurityEvent("SECURITY_INITIALIZED", $"Security service initialized with {_authorizedUsers.Count} authorized users");
            }
            else
            {
                // First time setup - no default password
                Debug.WriteLine("[Security] First-time setup detected - master password must be configured");
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

    /// <summary>
    /// Sets the initial master password during first-time setup.
    /// This method can only be called when IsFirstTimeSetup is true.
    /// The password must meet strong password requirements.
    /// 
    /// <para><strong>Security Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item>Only call this method when IsFirstTimeSetup is true</item>
    /// <item>Validate the password meets complexity requirements before calling</item>
    /// <item>Store the password securely (never in plain text)</item>
    /// <item>Consider prompting for password confirmation before calling</item>
    /// </list>
    /// </summary>
    /// <param name="password">The initial master password to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when not in first-time setup mode.</exception>
    /// <exception cref="ArgumentException">Thrown when the password doesn't meet security requirements.</exception>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetInitialPasswordAsync(string password)
    {
        if (!IsFirstTimeSetup)
        {
            throw new InvalidOperationException("Initial password can only be set during first-time setup.");
        }

        // Validate password strength
        PasswordValidator.ValidatePasswordOrThrow(password);

        _masterPassword = password;
        IsFirstTimeSetup = false;

        // Mark as initialized in settings
        var appSettings = _settingsService.LoadSettings();
        appSettings.IsInitialPasswordSet = true;
        _settingsService.SaveSettings(appSettings);

        // Save to encrypted storage
        await SaveSecurityConfigAsync();

        Debug.WriteLine("[Security] Initial master password set successfully");
        LogSecurityEvent("INITIAL_PASSWORD_SET", "Initial master password configured");
    }

    /// <summary>
    /// Saves the current security configuration to encrypted storage.
    /// This includes the master password and list of authorized users.
    /// 
    /// <para><strong>Storage Security:</strong></para>
    /// <para>
    /// Data is encrypted using Windows Data Protection API (DPAPI) with LOCAL=user scope.
    /// This means the encrypted data can only be decrypted by the same user on the same machine.
    /// The encryption key is derived from Windows credentials and is never stored in the application.
    /// </para>
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="Exception">Thrown when the security configuration cannot be saved.</exception>
    public async Task SaveSecurityConfigAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_masterPassword))
            {
                throw new InvalidOperationException("Cannot save security config without a master password");
            }

            var appSettings = _settingsService.LoadSettings();
            var configPath = _encryptedSettingsService.GetSecurityConfigPath(appSettings.SecurityConfigPath);

            await _encryptedSettingsService.SaveSecurityConfigAsync(configPath, _masterPassword, _authorizedUsers);

            Debug.WriteLine($"[Security] Security config saved to: {configPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR saving security config: {ex.Message}");
            LogSecurityEvent("SECURITY_SAVE_ERROR", $"Failed to save security configuration: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets the master password for the application.
    /// The password must meet strong password requirements.
    /// This method does NOT persist the password; call <see cref="SaveSecurityConfigAsync"/> to save changes.
    /// 
    /// <para><strong>Important:</strong></para>
    /// <para>Use <see cref="SetInitialPasswordAsync"/> for first-time setup instead of this method.</para>
    /// </summary>
    /// <param name="password">The new master password.</param>
    /// <exception cref="ArgumentException">Thrown when the password doesn't meet security requirements.</exception>
    public void SetMasterPassword(string password)
    {
        // Validate password strength
        PasswordValidator.ValidatePasswordOrThrow(password);
        
        _masterPassword = password;
        Debug.WriteLine("[Security] Master password updated (not persisted)");
    }

    /// <summary>
    /// Validates the provided password against the master password.
    /// If valid and not locked out, activates the master password override for the current session.
    /// 
    /// <para><strong>Rate Limiting:</strong></para>
    /// <para>
    /// After 5 failed attempts, authentication is locked out for 15 minutes.
    /// All failed attempts are logged to the audit log for security monitoring.
    /// </para>
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns><c>true</c> if the password is correct and not locked out; otherwise, <c>false</c>.</returns>
    public bool ValidateMasterPassword(string password)
    {
        // Check if locked out
        if (IsLockedOut)
        {
            var remainingTime = RemainingLockoutTime;
            Debug.WriteLine($"[Security] Authentication locked out - {remainingTime?.TotalMinutes:F1} minutes remaining");
            LogSecurityEvent(
                "AUTH_LOCKOUT_ATTEMPT",
                $"Authentication attempt while locked out - {remainingTime?.TotalMinutes:F1} minutes remaining"
            );
            return false;
        }

        bool isValid = password == _masterPassword;

        if (isValid)
        {
            // Successful authentication - reset failed attempts and activate override
            _failedPasswordAttempts = 0;
            _lockoutUntil = null;
            _isMasterPasswordOverrideActive = true;
            
            Debug.WriteLine("[Security] Master password validated successfully");
            LogSecurityEvent("MASTER_UNLOCK", "Master password override activated");
            
            return true;
        }
        else
        {
            // Failed authentication - increment counter
            _failedPasswordAttempts++;
            Debug.WriteLine($"[Security] Failed password attempt {_failedPasswordAttempts}/{MaxFailedAttempts}");
            
            LogSecurityEvent(
                "AUTH_FAILED",
                $"Failed master password attempt ({_failedPasswordAttempts}/{MaxFailedAttempts})"
            );

            // Check if lockout threshold reached
            if (_failedPasswordAttempts >= MaxFailedAttempts)
            {
                _lockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
                Debug.WriteLine($"[Security] LOCKOUT ACTIVATED - Authentication blocked until {_lockoutUntil}");
                
                LogSecurityEvent(
                    "AUTH_LOCKOUT_ACTIVATED",
                    $"Too many failed attempts - authentication locked out for {LockoutDuration.TotalMinutes} minutes"
                );
            }

            return false;
        }
    }

    /// <summary>
    /// Changes the master password after validating the old password.
    /// The new password must meet strong password requirements.
    /// The new password is persisted to encrypted storage automatically.
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// <list type="bullet">
    /// <item>Always validate the old password before allowing a change</item>
    /// <item>Enforce strong password requirements on the new password</item>
    /// <item>Log the password change event for audit purposes</item>
    /// <item>Consider forcing re-authentication after password change</item>
    /// </list>
    /// </summary>
    /// <param name="oldPassword">The current master password.</param>
    /// <param name="newPassword">The new master password to set.</param>
    /// <returns><c>true</c> if the password was changed successfully; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when the new password doesn't meet security requirements.</exception>
    public async Task<bool> ChangeMasterPasswordAsync(string oldPassword, string newPassword)
    {
        if (oldPassword != _masterPassword)
        {
            Debug.WriteLine("[Security] Password change failed - incorrect old password");
            LogSecurityEvent("PASSWORD_CHANGE_FAILED", "Failed to change master password - incorrect old password");
            return false;
        }

        // Validate new password strength
        if (!PasswordValidator.ValidatePassword(newPassword, out string errorMessage))
        {
            Debug.WriteLine($"[Security] Password change failed - {errorMessage}");
            LogSecurityEvent("PASSWORD_CHANGE_FAILED", $"Password change rejected - {errorMessage}");
            throw new ArgumentException(errorMessage, nameof(newPassword));
        }

        _masterPassword = newPassword;
        await SaveSecurityConfigAsync();
        
        Debug.WriteLine("[Security] Master password changed successfully");
        LogSecurityEvent("MASTER_PASSWORD_CHANGED", "Master password was changed");
        
        return true;
    }

    /// <summary>
    /// Adds a user to the authorized users list.
    /// The list is automatically saved to encrypted storage.
    /// 
    /// <para><strong>Authorization Model:</strong></para>
    /// <para>
    /// Authorized users are identified by their Windows username (Environment.UserName).
    /// Once added, these users have permanent access to restricted features without
    /// needing to enter the master password.
    /// </para>
    /// </summary>
    /// <param name="userId">The user ID to authorize (typically Windows username).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddAuthorizedUserAsync(string userId)
    {
        if (!_authorizedUsers.Contains(userId))
        {
            _authorizedUsers.Add(userId);
            await SaveSecurityConfigAsync();
            
            Debug.WriteLine($"[Security] Added authorized user: {userId}");
            LogSecurityEvent("USER_ADDED", $"User '{userId}' added to authorized users list");
        }
    }

    /// <summary>
    /// Removes a user from the authorized users list.
    /// The list is automatically saved to encrypted storage.
    /// </summary>
    /// <param name="userId">The user ID to remove from authorization.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveAuthorizedUserAsync(string userId)
    {
        if (_authorizedUsers.Remove(userId))
        {
            await SaveSecurityConfigAsync();
            
            Debug.WriteLine($"[Security] Removed authorized user: {userId}");
            LogSecurityEvent("USER_REMOVED", $"User '{userId}' removed from authorized users list");
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
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetAuthorizedUsersAsync(List<string> users)
    {
        _authorizedUsers = users ?? new();
        await SaveSecurityConfigAsync();
        
        Debug.WriteLine($"[Security] Authorized users list updated - Count: {_authorizedUsers.Count}");
        LogSecurityEvent("AUTHORIZED_USERS_UPDATED", $"Authorized users list updated - Count: {_authorizedUsers.Count}");
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
        Debug.WriteLine("[Security] Master password override deactivated");
        LogSecurityEvent("MASTER_LOCK", "Master password override deactivated");
    }

    /// <summary>
    /// Logs a security event to the audit log.
    /// </summary>
    /// <param name="actionType">The type of security action.</param>
    /// <param name="description">Description of the security event.</param>
    private void LogSecurityEvent(string actionType, string description)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                ActionType = actionType,
                Description = description,
                UserId = CurrentUserId,
                TargetPath = "SECURITY_SYSTEM",
                Details = ""
            };

            _auditLoggingService.LogAction(entry);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR logging security event: {ex.Message}");
        }
    }
}