using AIM.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AIM.ViewModels;

/// <summary>
/// ViewModel for the Log Viewer page.
/// Displays basic application logs.
/// </summary>
public partial class LogViewerViewModel : ObservableObject
{
    /// <summary>
    /// Gets the collection of audit log entries to display.
    /// </summary>
    public ObservableCollection<LogEntry> AuditLogs { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LogViewerViewModel"/> class.
    /// </summary>
    public LogViewerViewModel()
    {
        // Basic implementation - no audit logging available
        var welcomeEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            UserName = Environment.UserName,
            Action = "LOG_VIEWER_OPENED",
            Details = "Log viewer initialized"
        };
        AuditLogs.Add(welcomeEntry);
    }
}
