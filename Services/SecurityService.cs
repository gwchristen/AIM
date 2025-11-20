using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Text.Json;
using AIM.Models;


namespace AIM.Services;

/// <summary>
/// Provides comprehensive security services for authentication, authorization, and user access control.
/// 
/// <para><strong>Security Architecture:</strong></para>
/// <para>
/// This service implements a database-driven authorization model with fallback to file-based security:
/// 1. <strong>Database Authorization (Primary):</strong> Users are checked against a centralized SQLite database.
///    - Users in the database get their assigned access level (Admin, SuperAdmin, etc.)
///    - Users not in the database get Basic access level automatically
///    - No blocking dialogs or first-time setup required
/// 2. <strong>File-Based Authorization (Fallback):</strong> If database is not configured or unavailable,
///    falls back to encrypted file-based authorized users list
/// 3. <strong>Master Password Override (Optional):</strong> A master password can temporarily grant full access
/// </para>
/// 
/// <para><strong>Authentication Flow:</strong></para>
/// <list type="number">
/// <item>On launch, users are identified by their Windows username (Environment.UserName).</item>
/// <item>If database is configured:
///   <list type="bullet">
///     <item>Check if user exists in authorized_users table</item>
///     <item>If yes: Grant user's access level from database (typically Admin or SuperAdmin)</item>
///     <item>If no: Grant Basic access level (no blocking, just reduced features)</item>
///   </list>
/// </item>
/// <item>If database is not available, fall back to file-based authorization</item>
/// <item>The master password override can optionally be activated for temporary SuperAdmin access</item>
/// </list>
/// 
/// <para><strong>Access Levels:</strong></para>
/// <list type="bullet">
/// <item>1 = Basic: Can use core app features, limited admin functionality</item>
/// <item>2 = Admin: Can access all features including Inventory and user management</item>
/// <item>3 = SuperAdmin: Full access including security settings (via master password override)</item>
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
/// <item>Database-driven centralized user management</item>
/// <item>Automatic Basic access for all users (no blocking)</item>
/// <item>Rate limiting: 5 failed password attempts trigger a 15-minute lockout</item>
/// <item>All authentication attempts are logged to the audit log</item>
/// <item>No hardcoded or default passwords</item>
/// <item>No first-time setup blocking - app is always usable</item>
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
    private readonly IEncryptedSettingsService _encryptedSettingsService;
    private readonly ISettingsService _settingsService;
    private readonly AuditLoggingService _auditLoggingService;
    private DatabaseSecurityService? _databaseSecurityService;
    private System.Threading.Timer? _refreshTimer;
    
    private string? _masterPassword;
    private List<string> _authorizedUsers = new();
    private Dictionary<string, int> _userAccessLevels = new(); // Maps username to access level
    private bool _isMasterPasswordOverrideActive;
    
    // Rate limiting state
    private int _failedPasswordAttempts;
    private DateTime? _lockoutUntil;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    // Masked constant - Base64-encoded UNC path for fallback
    private const string MaskedSharedSecurityPath = "XFxvaDFjYW0wMVxjbWxcSW50ZXJuYWxcTEFCIFNUT0NLXEltcG9ydGFudCBJbnZlbnRvcnkgUmVsYXRlZCBEb2N1bWVudHNcQUlNXEFJTV9TZWN1cml0eVxzZWN1cml0eS5jb25maWc=";
    private const string SecurityConfigFileName = "security-config.ini";
    
    /// <summary>
    /// Gets the current user ID based on the Windows account name.
    /// This is used to determine if the current user is in the authorized users list.
    /// </summary>
    public string CurrentUserId { get; private set; }

    /// <summary>
    /// Gets whether the application is fully unlocked via master password override or authorized user status.
    /// When true, the user has access to all restricted features in the application.
    /// In the new model, this checks if the user has Admin or SuperAdmin access level.
    /// </summary>
    public bool IsFullyUnlocked => _isMasterPasswordOverrideActive || IsCurrentUserAdmin();

    /// <summary>
    /// Gets whether the master password override is currently active.
    /// The override remains active until explicitly deactivated or the application is restarted.
    /// </summary>
    public bool IsMasterPasswordOverrideActive => _isMasterPasswordOverrideActive;

    /// <summary>
    /// Gets whether the application is in first-time setup mode.
    /// In the new database-driven model, this is always false - all users can use the app.
    /// Users get Basic privileges by default, and Admin privileges if in the database.
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

    /// <summary>
    /// Gets the shared security configuration path using a priority chain:
    /// 1. From security-config.ini file (highest priority - admin can update without recompiling)
    /// 2. From masked constant (Base64-encoded fallback)
    /// 3. From AppSettings.SharedSecurityConfigPath (lowest priority)
    /// </summary>
    /// <param name="appSettings">The application settings.</param>
    /// <returns>The shared security path, or null if not configured or UseSharedConfig is false.</returns>
    private string? GetSharedPath(AppSettings appSettings)
    {
        // Check if shared config is enabled
        if (!appSettings.UseSharedConfig)
        {
            Debug.WriteLine("[Security] Shared config is disabled in AppSettings");
            return null;
        }

        // Priority 1: Try to load from security-config.ini file
        var configFilePath = GetSharedPathFromConfigFile();
        if (!string.IsNullOrWhiteSpace(configFilePath))
        {
            Debug.WriteLine($"[Security] Using shared path from config file: {configFilePath}");
            return configFilePath;
        }

        // Priority 2: Try masked constant
        var maskedPath = GetSharedPathFromMaskedConstant();
        if (!string.IsNullOrWhiteSpace(maskedPath))
        {
            Debug.WriteLine($"[Security] Using shared path from masked constant");
            return maskedPath;
        }

        // Priority 3: Try AppSettings
        if (!string.IsNullOrWhiteSpace(appSettings.SharedSecurityConfigPath))
        {
            Debug.WriteLine($"[Security] Using shared path from AppSettings: {appSettings.SharedSecurityConfigPath}");
            return appSettings.SharedSecurityConfigPath;
        }

        Debug.WriteLine("[Security] No shared path configured in any source");
        return null;
    }

    /// <summary>
    /// Reads the shared security path from the security-config.ini file.
    /// The file should be located in the application root directory.
    /// </summary>
    /// <returns>The shared security path from the config file, or null if not found or invalid.</returns>
    private string? GetSharedPathFromConfigFile()
    {
        try
        {
            // Look for security-config.ini in the application directory
            var appDirectory = AppContext.BaseDirectory;
            var configPath = Path.Combine(appDirectory, SecurityConfigFileName);

            if (!File.Exists(configPath))
            {
                Debug.WriteLine($"[Security] Config file not found at: {configPath}");
                return null;
            }

            Debug.WriteLine($"[Security] Reading config file: {configPath}");
            var lines = File.ReadAllLines(configPath);

            foreach (var line in lines)
            {
                // Skip comments and empty lines
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // Look for SharedSecurityPath=value
                if (trimmedLine.StartsWith("SharedSecurityPath=", StringComparison.OrdinalIgnoreCase))
                {
                    var path = trimmedLine.Substring("SharedSecurityPath=".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        Debug.WriteLine($"[Security] Found SharedSecurityPath in config file");
                        return path;
                    }
                }
            }

            Debug.WriteLine("[Security] SharedSecurityPath not found in config file");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] Error reading config file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Decodes the Base64-encoded masked constant to get the shared security path.
    /// This provides a fallback when the config file is not present.
    /// </summary>
    /// <returns>The decoded shared security path, or null if decoding fails.</returns>
    private string? GetSharedPathFromMaskedConstant()
    {
        try
        {
            var bytes = Convert.FromBase64String(MaskedSharedSecurityPath);
            var path = Encoding.UTF8.GetString(bytes);
            Debug.WriteLine("[Security] Successfully decoded masked constant");
            return path;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] Error decoding masked constant: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Asynchronously initializes the security service by loading user authorization from database or files.
    /// This method MUST be called after construction and before using any security features.
    /// 
    /// <para><strong>Initialization Flow (Database Mode):</strong></para>
    /// <list type="number">
    /// <item>Checks if database path is configured in settings.json</item>
    /// <item>Connects to the centralized security database</item>
    /// <item>Looks up current Windows user in authorized_users table</item>
    /// <item>If user exists: Grants user's access level from database</item>
    /// <item>If user does not exist: Grants Basic access level (no blocking)</item>
    /// <item>Sets IsFirstTimeSetup to false (no blocking setup required)</item>
    /// </list>
    /// 
    /// <para><strong>Initialization Flow (File-Based Fallback):</strong></para>
    /// <list type="number">
    /// <item>Attempts to load shared network security config</item>
    /// <item>Falls back to local user-specific config if network unavailable</item>
    /// <item>If config exists: Checks if user is in authorized users list</item>
    /// <item>If no config: Grants Basic access level to allow app usage</item>
    /// <item>Sets IsFirstTimeSetup to false (no blocking setup required)</item>
    /// </list>
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            var appSettings = _settingsService.LoadSettings();

            // Check if database security is configured
            if (!string.IsNullOrWhiteSpace(appSettings.SecurityDatabasePath))
            {
                Debug.WriteLine($"[Security] Database security path configured: {appSettings.SecurityDatabasePath}");
                
                // Check if database file exists
                if (!File.Exists(appSettings.SecurityDatabasePath))
                {
                    Debug.WriteLine($"[Security] Database file not found at: {appSettings.SecurityDatabasePath}");
                    Debug.WriteLine("[Security] Granting Basic privileges - no database available");
                    
                    // Grant Basic privileges to allow app usage
                    IsFirstTimeSetup = false;
                    _userAccessLevels[CurrentUserId] = 1; // Basic access
                    LogSecurityEvent("SECURITY_INITIALIZED", $"Security initialized with Basic privileges (no database)");
                    return;
                }
                else
                {
                    try
                    {
                        await InitializeDatabaseSecurityAsync(appSettings.SecurityDatabasePath);
                    
                        // Check if current user exists in database
                        var currentUser = await _databaseSecurityService?.GetUserByUsernameAsync(CurrentUserId);
                        if (currentUser != null && currentUser.IsActive)
                        {
                            // User exists in database - grant their access level (typically Admin or SuperAdmin)
                            _userAccessLevels[CurrentUserId] = currentUser.AccessLevel;
                            Debug.WriteLine($"[Security] User '{CurrentUserId}' found in database with AccessLevel {currentUser.AccessLevel}");
                            LogSecurityEvent("SECURITY_INITIALIZED", $"User '{CurrentUserId}' authenticated with AccessLevel {currentUser.AccessLevel}");
                        }
                        else
                        {
                            // User does not exist in database - grant Basic privileges
                            _userAccessLevels[CurrentUserId] = 1; // Basic access
                            Debug.WriteLine($"[Security] User '{CurrentUserId}' not found in database - granting Basic privileges");
                            LogSecurityEvent("SECURITY_INITIALIZED", $"User '{CurrentUserId}' granted Basic privileges (not in database)");
                        }

                        // No first-time setup required - all users can use the app
                        IsFirstTimeSetup = false;
                        
                        // Load master password from database (optional for override functionality)
                        var masterPasswordHash = await _databaseSecurityService?.GetSecuritySettingAsync("MasterPasswordHash");
                        if (!string.IsNullOrEmpty(masterPasswordHash))
                        {
                            _masterPassword = masterPasswordHash; // Store the hash for validation
                            Debug.WriteLine("[Security] Loaded master password from database");
                        }

                        return; // Successfully initialized with database
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Security] Database initialization failed: {ex.Message}");
                        Debug.WriteLine("[Security] Granting Basic privileges - database error");
                        
                        // Grant Basic privileges to allow app usage even if database fails
                        IsFirstTimeSetup = false;
                        _userAccessLevels[CurrentUserId] = 1; // Basic access
                        LogSecurityEvent("SECURITY_INITIALIZED", $"Security initialized with Basic privileges (database error)");
                        return;
                    }
                }
            }

            // Fall back to file-based security (legacy)
            Debug.WriteLine("[Security] Using file-based security (legacy mode)");
            var configPath = _encryptedSettingsService.GetSecurityConfigPath(appSettings.SecurityConfigPath);

            // Get shared network path using priority chain
            string? sharedNetworkPath = GetSharedPath(appSettings);

            Debug.WriteLine($"[Security] Checking for shared network config at: {sharedNetworkPath ?? "not configured"}");

            var securityData = null as EncryptedSettingsService.SecurityData;
            bool loadedFromSharedConfig = false;

            // Deobfuscate passphrase if provided in settings
            string? passphrase = null;
            if (!string.IsNullOrEmpty(appSettings.Passphrase))
            {
                try
                {
                    passphrase = DeobfuscatePassphrase(appSettings.Passphrase);
                    Debug.WriteLine("[Security] Deobfuscated passphrase from settings");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Security] Warning: Could not deobfuscate passphrase: {ex.Message}");
                }
            }

            // Try shared network config first (if configured)
            if (!string.IsNullOrWhiteSpace(sharedNetworkPath) && File.Exists(sharedNetworkPath))
            {
                try
                {
                    Debug.WriteLine($"[Security] Found shared network security config, attempting to load...");
                    securityData = await _encryptedSettingsService.LoadSecurityConfigAsync(sharedNetworkPath, passphrase);

                    if (securityData != null)
                    {
                        Debug.WriteLine($"[Security] Successfully loaded security config from shared network");
                        loadedFromSharedConfig = true;

                        // Also cache it locally for offline access
                        try
                        {
                            await _encryptedSettingsService.SaveSecurityConfigAsync(configPath, securityData.MasterPassword, securityData.AuthorizedUsers, passphrase);
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
                try
                {
                    securityData = await _encryptedSettingsService.LoadSecurityConfigAsync(configPath, passphrase);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Security] Could not load local config: {ex.Message}");
                }
            }

            if (securityData != null)
            {
                // Configuration exists - load it
                _masterPassword = securityData.MasterPassword;
                _authorizedUsers = securityData.AuthorizedUsers ?? new();
                
                // Check if current user is in authorized users list
                if (IsCurrentUserAuthorized())
                {
                    // User is authorized - grant Admin privileges
                    _userAccessLevels[CurrentUserId] = 2; // Admin access
                    Debug.WriteLine($"[Security] User '{CurrentUserId}' found in authorized users - granting Admin privileges");
                }
                else
                {
                    // User not authorized - grant Basic privileges
                    _userAccessLevels[CurrentUserId] = 1; // Basic access
                    Debug.WriteLine($"[Security] User '{CurrentUserId}' not in authorized users - granting Basic privileges");
                }
                
                // No first-time setup required
                IsFirstTimeSetup = false;

                string configSource = loadedFromSharedConfig ? "shared network" : "local";
                Debug.WriteLine($"[Security] Loaded {_authorizedUsers.Count} authorized users from {configSource} encrypted config");
                LogSecurityEvent("SECURITY_INITIALIZED", $"Security service initialized with {_authorizedUsers.Count} authorized users from {configSource}");
            }
            else
            {
                // No configuration found - grant Basic privileges to allow app usage
                Debug.WriteLine("[Security] No security configuration found - granting Basic privileges");
                IsFirstTimeSetup = false;
                _masterPassword = null;
                _authorizedUsers = new();
                _userAccessLevels[CurrentUserId] = 1; // Basic access

                LogSecurityEvent("SECURITY_INITIALIZED", "Security initialized with Basic privileges (no config found)");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR initializing security: {ex.Message}");
            LogSecurityEvent("SECURITY_INIT_ERROR", $"Failed to initialize security: {ex.Message}");

            // On error, grant Basic privileges to allow app usage
            IsFirstTimeSetup = false;
            _masterPassword = null;
            _authorizedUsers = new();
            _userAccessLevels[CurrentUserId] = 1; // Basic access
            
            Debug.WriteLine("[Security] Granted Basic privileges due to initialization error");
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
    /// Sets the complete authorized users list, replacing any existing users (synchronous version).
    /// This method does NOT persist the list to storage. Use for loading from settings only.
    /// For persistent changes, use <see cref="SetAuthorizedUsersAsync"/> instead.
    /// </summary>
    /// <param name="users">The new list of authorized users, or null to clear the list.</param>
    public void SetAuthorizedUsers(List<string> users)
    {
        _authorizedUsers = users ?? new();
        Debug.WriteLine($"[Security] Authorized users list set (not persisted) - Count: {_authorizedUsers.Count}");
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
    /// Gets the access level of the current user.
    /// </summary>
    /// <returns>The access level (1=Basic, 2=Admin, 3=SuperAdmin).</returns>
    public int GetCurrentUserAccessLevel()
    {
        if (_isMasterPasswordOverrideActive)
        {
            return 3; // Master password override gives SuperAdmin access
        }

        if (_userAccessLevels.TryGetValue(CurrentUserId, out int accessLevel))
        {
            return accessLevel;
        }

        return 1; // Default to Basic access if not found
    }

    /// <summary>
    /// Checks if the current user has admin access (level 2 or higher).
    /// </summary>
    /// <returns><c>true</c> if the user has admin access; otherwise, <c>false</c>.</returns>
    public bool IsCurrentUserAdmin()
    {
        return GetCurrentUserAccessLevel() >= 2;
    }

    /// <summary>
    /// Checks if the current user has super admin access (level 3).
    /// </summary>
    /// <returns><c>true</c> if the user has super admin access; otherwise, <c>false</c>.</returns>
    public bool IsCurrentUserSuperAdmin()
    {
        return GetCurrentUserAccessLevel() >= 3;
    }

    /// <summary>
    /// Initializes the database security service if a database path is configured.
    /// </summary>
    private async Task InitializeDatabaseSecurityAsync(string databasePath)
    {
        try
        {
            _databaseSecurityService = new DatabaseSecurityService(databasePath);
            await _databaseSecurityService.InitializeDatabaseAsync();

            // Load users from database
            await RefreshUsersFromDatabaseAsync();

            // Start periodic refresh timer
            _refreshTimer = new System.Threading.Timer(
                async _ => await RefreshUsersFromDatabaseAsync(),
                null,
                RefreshInterval,
                RefreshInterval
            );

            Debug.WriteLine("[Security] Database security service initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR initializing database security: {ex.Message}");
            _databaseSecurityService = null;
        }
    }

    /// <summary>
    /// Refreshes the authorized users list from the database.
    /// Preserves the current user's Basic access level if they're not in the database.
    /// </summary>
    private async Task RefreshUsersFromDatabaseAsync()
    {
        if (_databaseSecurityService == null)
            return;

        try
        {
            var users = await _databaseSecurityService.GetAuthorizedUsersAsync();
            
            // Save current user's access level if they have Basic access (not in database)
            bool currentUserHasBasicAccess = false;
            if (_userAccessLevels.TryGetValue(CurrentUserId, out int currentLevel) && currentLevel == 1)
            {
                currentUserHasBasicAccess = true;
            }
            
            _authorizedUsers.Clear();
            _userAccessLevels.Clear();

            foreach (var user in users.Where(u => u.IsActive))
            {
                _authorizedUsers.Add(user.Username);
                _userAccessLevels[user.Username] = user.AccessLevel;
            }
            
            // Restore Basic access for current user if they're not in database
            if (currentUserHasBasicAccess && !_userAccessLevels.ContainsKey(CurrentUserId))
            {
                _userAccessLevels[CurrentUserId] = 1; // Restore Basic access
                Debug.WriteLine($"[Security] Preserved Basic access for user '{CurrentUserId}' (not in database)");
            }

            Debug.WriteLine($"[Security] Refreshed {_authorizedUsers.Count} users from database");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] ERROR refreshing users from database: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the database security service instance.
    /// </summary>
    public DatabaseSecurityService? GetDatabaseSecurityService()
    {
        return _databaseSecurityService;
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

    /// <summary>
    /// Deobfuscates a passphrase that was obfuscated by the installer.
    /// This uses simple XOR with a key and Base64 encoding - NOT cryptographic protection.
    /// The obfuscation only prevents casual discovery of the passphrase in configuration files.
    /// </summary>
    /// <param name="obfuscated">The obfuscated passphrase string</param>
    /// <returns>The deobfuscated passphrase</returns>
    private string DeobfuscatePassphrase(string obfuscated)
    {
        if (string.IsNullOrEmpty(obfuscated))
            return string.Empty;

        try
        {
            // Simple XOR key - must match the one used in the installer
            byte[] xorKey = new byte[] { 0xA5, 0x3C, 0x7E, 0x91, 0x42, 0xF8, 0x6D, 0x2B };
            
            byte[] data = Convert.FromBase64String(obfuscated);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= xorKey[i % xorKey.Length];
            }
            
            return Encoding.UTF8.GetString(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Security] Warning: Could not deobfuscate passphrase: {ex.Message}");
            return string.Empty;
        }
    }
}