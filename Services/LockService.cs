using System;
using System.Diagnostics;

namespace AIM.Services;

/// <summary>
/// Service for managing application lock state with PIN protection.
/// PIN is 5300.
/// </summary>
public class LockService : ILockService
{
    private const string CorrectPin = "5300";
    private bool _isLocked = false;

    /// <summary>
    /// Gets whether the application is currently locked.
    /// </summary>
    public bool IsLocked => _isLocked;

    /// <summary>
    /// Occurs when the lock state changes.
    /// </summary>
    public event EventHandler<bool>? LockStateChanged;

    /// <summary>
    /// Attempts to unlock the application with the provided PIN.
    /// </summary>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if the PIN is correct and the application is unlocked; otherwise, false.</returns>
    public bool Unlock(string pin)
    {
        if (pin == CorrectPin)
        {
            _isLocked = false;
            Debug.WriteLine("[LockService] Application unlocked");
            LockStateChanged?.Invoke(this, _isLocked);
            return true;
        }
        Debug.WriteLine("[LockService] Invalid PIN attempt");
        return false;
    }

    /// <summary>
    /// Locks the application.
    /// </summary>
    public void Lock()
    {
        _isLocked = true;
        Debug.WriteLine("[LockService] Application locked");
        LockStateChanged?.Invoke(this, _isLocked);
    }
}
