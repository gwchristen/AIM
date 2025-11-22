using System;

namespace AIM.Services
{
    public class LockStateChangedEventArgs : EventArgs
    {
        public bool IsLocked { get; set; }

        public LockStateChangedEventArgs(bool isLocked)
        {
            IsLocked = isLocked;
        }
    }
}