using System;
using System.Diagnostics;

namespace AIM.Services
{
    public class LockService : ILockService
    {
        private const string CorrectPin = "5300";
        private bool _isLocked = false;

        public bool IsLocked => _isLocked;

        public event EventHandler<LockStateChangedEventArgs>? LockStateChanged;

        public bool Unlock(string pin)
        {
            if (pin == CorrectPin)
            {
                _isLocked = false;
                Debug.WriteLine("[LockService] Application unlocked");
                LockStateChanged?.Invoke(this, new LockStateChangedEventArgs(_isLocked));
                return true;
            }
            Debug.WriteLine("[LockService] Invalid PIN attempt");
            return false;
        }

        public void Lock()
        {
            _isLocked = true;
            Debug.WriteLine("[LockService] Application locked");
            LockStateChanged?.Invoke(this, new LockStateChangedEventArgs(_isLocked));
        }
    }
}