using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AIM.Services;

public class InfoBarService : IInfoBarService
{
    private InfoBar _infoBar;
    private DispatcherQueue _dispatcherQueue;

    public void Initialize(InfoBar infoBar)
    {
        _infoBar = infoBar;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, int autoHideDelay = 5000)
    {
        // Ensure the call is made on the UI thread
        _dispatcherQueue.TryEnqueue(async () =>
        {
            _infoBar.Title = title;
            _infoBar.Message = message;
            _infoBar.Severity = severity;
            _infoBar.IsOpen = true;

            if (autoHideDelay > 0)
            {
                await Task.Delay(autoHideDelay);
                // Check if it's still the same message before closing
                if (_infoBar.Title == title && _infoBar.Message == message)
                {
                    _infoBar.IsOpen = false;
                }
            }
        });
    }
}