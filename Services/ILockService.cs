namespace AIM.Services;

/// <summary>
/// Service for managing application lock state with PIN protection.
/// </summary>
public interface ILockService
{
    /// <summary>
    /// Gets whether the application is currently locked.
    /// </summary>
    bool IsLocked { get; }

    /// <summary>
    /// Attempts to unlock the application with the provided PIN.
    /// </summary>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if the PIN is correct and the application is unlocked; otherwise, false.</returns>
    bool Unlock(string pin);

    /// <summary>
    /// Locks the application.
    /// </summary>
    void Lock();

    /// <summary>
    /// Occurs when the lock state changes.
    /// </summary>
    event EventHandler<bool>? LockStateChanged;
}
