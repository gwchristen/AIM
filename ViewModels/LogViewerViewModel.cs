using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly ILockService _lockService;

    /// <summary>
    /// Gets the collection of audit log entries to display.
    /// </summary>
    public ObservableCollection<LogEntry> AuditLogs { get; } = new();

    /// <summary>
    /// Gets whether the Clear Logs button is enabled (not locked).
    /// </summary>
    [ObservableProperty]
    private bool isClearLogsEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogViewerViewModel"/> class.
    /// </summary>
    public LogViewerViewModel(ILockService lockService)
    {
        _lockService = lockService;

        // Subscribe to lock state changes
        _lockService.LockStateChanged += OnLockStateChanged;
        IsClearLogsEnabled = !_lockService.IsLocked;

        // Basic implementation - no audit logging available
        var welcomeEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserName = Environment.UserName,
            Action = "LOG_VIEWER_OPENED",
            Details = "Log viewer initialized"
        };
        AuditLogs.Add(welcomeEntry);
    }

    private void OnLockStateChanged(object? sender, bool isLocked)
    {
        IsClearLogsEnabled = !isLocked;
    }

    /// <summary>
    /// Clears all logs.
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        AuditLogs.Clear();
    }
}
