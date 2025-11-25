using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AIM.Services;

public class InfoBarService : IInfoBarService
{
    private InfoBar? _appInfoBar;
    private Timer? _timer;

    public void Initialize(InfoBar infoBar)
    {
        _appInfoBar = infoBar;
        // Using System.Threading.Timer is safer across different threads.
        _timer = new Timer(_ => Hide(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Show(string title, string message, InfoBarSeverity severity, int autoHideDelay = 5000)
    {
        // THE FIX: We no longer try to auto-find the control. We just check if it's there.
        if (_appInfoBar == null)
        {
            return; // If not initialized, do nothing. This prevents the crash.
        }

        // Ensure this UI update happens on the UI thread.
        _appInfoBar.DispatcherQueue.TryEnqueue(() =>
        {
            _appInfoBar.Title = title;
            _appInfoBar.Message = message;
            _appInfoBar.Severity = severity;
            _appInfoBar.IsOpen = true;

            // Stop any previous timer and start a new one if needed.
            _timer?.Change(autoHideDelay > 0 ? autoHideDelay : Timeout.Infinite, Timeout.Infinite);
        });
    }

    private void Hide()
    {
        if (_appInfoBar == null) return;

        _appInfoBar.DispatcherQueue.TryEnqueue(() =>
        {
            _appInfoBar.IsOpen = false;
        });
    }
}