using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Manages application security using a PIN-based access control system.
/// 
/// This service provides:
/// - PIN validation for accessing restricted features
/// - Session-based unlock state (locked/unlocked)
/// - Access control for Inventory Tab, directory selectors, and clear logs
/// </summary>
public class SecurityService
{
    private readonly ISettingsService _settingsService;

    // Hardcoded PIN - change this value as needed
    private const string HARDCODED_PIN = "1234";

    private bool _isSessionUnlocked;

    /// <summary>
    /// Gets the current Windows username.
    /// </summary>
    public string CurrentUserId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current session is fully unlocked.
    /// When unlocked, users can access: Inventory Tab, directory selectors in settings, and clear logs button.
    /// </summary>
    public bool IsFullyUnlocked => _isSessionUnlocked;

    /// <summary>
    /// Initializes a new instance of the SecurityService class.
    /// </summary>
    /// <param name="settingsService">Service for managing application settings</param>
    public SecurityService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        CurrentUserId = Environment.UserName;
        _isSessionUnlocked = false;

        Debug.WriteLine($"[Security] SecurityService initialized - Current user: {CurrentUserId}");
    }

    /// <summary>
    /// Validates the provided PIN and unlocks the session if correct.
    /// </summary>
    /// <param name="pin">The PIN to validate</param>
    /// <returns>True if the PIN is correct and session is unlocked; otherwise false</returns>
    public bool ValidatePin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            _isSessionUnlocked = false;
            return false;
        }

        _isSessionUnlocked = pin == HARDCODED_PIN;

        if (_isSessionUnlocked)
        {
            Debug.WriteLine($"[Security] Session unlocked by user: {CurrentUserId}");
        }
        else
        {
            Debug.WriteLine($"[Security] Failed unlock attempt by user: {CurrentUserId}");
        }

        return _isSessionUnlocked;
    }

    /// <summary>
    /// Changes the hardcoded PIN after validating the current one.
    /// </summary>
    /// <param name="oldPin">The current PIN for validation</param>
    /// <param name="newPin">The new PIN to set</param>
    /// <returns>True if the old PIN is correct and change was successful; otherwise false</returns>
    public bool ChangePin(string oldPin, string newPin)
    {
        if (string.IsNullOrWhiteSpace(oldPin) || string.IsNullOrWhiteSpace(newPin))
        {
            return false;
        }

        if (oldPin != HARDCODED_PIN)
        {
            Debug.WriteLine($"[Security] PIN change failed - incorrect old PIN");
            return false;
        }

        if (newPin.Length < 4)
        {
            Debug.WriteLine($"[Security] PIN change failed - new PIN must be at least 4 digits");
            return false;
        }

        Debug.WriteLine($"[Security] PIN change attempted - please update HARDCODED_PIN constant in SecurityService. cs");

        return false;
    }

    /// <summary>
    /// Locks the current session, requiring PIN re-entry to access restricted features.
    /// </summary>
    public void LockSession()
    {
        _isSessionUnlocked = false;
        Debug.WriteLine($"[Security] Session locked by user: {CurrentUserId}");
    }

    /// <summary>
    /// Verifies if the provided PIN is correct without unlocking the session.
    /// </summary>
    /// <param name="pin">The PIN to verify</param>
    /// <returns>True if the PIN is correct; otherwise false</returns>
    public bool VerifyPin(string pin)
    {
        return !string.IsNullOrWhiteSpace(pin) && pin == HARDCODED_PIN;
    }
}