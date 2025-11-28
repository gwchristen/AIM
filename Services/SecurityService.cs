using System;
using System.Diagnostics;

namespace AIM.Services;

/// <summary>
/// Manages application security using a PIN-based access control system.
/// </summary>
public class SecurityService
{
    private readonly ISettingsService _settingsService;

    private const string HARDCODED_PIN = "1234";

    private bool _isSessionUnlocked;

    /// <summary>
    /// Event fired when the lock state changes (locked or unlocked).
    /// </summary>
    public event EventHandler<bool> LockStateChanged;

    /// <summary>
    /// Gets the current Windows username.
    /// </summary>
    public string CurrentUserId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current session is fully unlocked.
    /// </summary>
    public bool IsFullyUnlocked => _isSessionUnlocked;

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
            OnLockStateChanged(true);
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

        Debug.WriteLine($"[Security] PIN change attempted - please update HARDCODED_PIN constant in SecurityService.cs");

        return false;
    }

    /// <summary>
    /// Locks the current session, requiring PIN re-entry to access restricted features.
    /// </summary>
    public void LockSession()
    {
        _isSessionUnlocked = false;
        Debug.WriteLine($"[Security] Session locked by user: {CurrentUserId}");
        OnLockStateChanged(false);
    }

    /// <summary>
    /// Verifies if the provided PIN is correct without unlocking the session. 
    /// </summary>
    public bool VerifyPin(string pin)
    {
        return !string.IsNullOrWhiteSpace(pin) && pin == HARDCODED_PIN;
    }

    /// <summary>
    /// Raises the LockStateChanged event.
    /// </summary>
    private void OnLockStateChanged(bool isUnlocked)
    {
        LockStateChanged?.Invoke(this, isUnlocked);
    }
}