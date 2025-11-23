using Microsoft.UI.Xaml.Controls;

namespace AIM.Services;

public interface IInfoBarService
{
    /// <summary>
    /// Initializes the service with a reference to the UI's InfoBar control.
    /// </summary>
    void Initialize(InfoBar infoBar);

    /// <summary>
    /// Displays a notification message to the user.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The content of the notification.</param>
    /// <param name="severity">The type of notification (e.g., Success, Warning, Error).</param>
    /// <param name="autoHideDelay">Optional: The time in milliseconds before the message automatically disappears. A value of 0 means it will not auto-hide.</param>
    void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, int autoHideDelay = 5000);
}