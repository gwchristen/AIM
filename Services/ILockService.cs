using System;

namespace AIM.Services
{
    public interface ILockService
    {
        bool IsLocked { get; }

        bool Unlock(string pin);

        void Lock();

        /// <summary>
        /// Occurs when the lock state changes.
        /// </summary>
        event EventHandler<LockStateChangedEventArgs>? LockStateChanged;
    }
}