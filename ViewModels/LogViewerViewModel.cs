using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AIM.Services;

namespace AIM.ViewModels;

/// <summary>
/// ViewModel for the Log Viewer page.
/// Displays audit logs from the Serilog audit logging system.
/// </summary>
public partial class LogViewerViewModel : ObservableObject
{
    private readonly ILockService _lockService;
    private readonly IAuditLoggingService _auditLoggingService;

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
    public LogViewerViewModel(ILockService lockService, IAuditLoggingService auditLoggingService)
    {
        _lockService = lockService;
        _auditLoggingService = auditLoggingService;

        // Subscribe to lock state changes
        _lockService.LockStateChanged += OnLockStateChanged;
        IsClearLogsEnabled = !_lockService.IsLocked;

        // Load audit logs from file
        _ = LoadAuditLogsAsync();
    }

    /// <summary>
    /// Loads audit logs from the Serilog file.
    /// </summary>
    private async Task LoadAuditLogsAsync()
    {
        try
        {
            var logs = await _auditLoggingService.ReadAuditLogsAsync(1000);
            AuditLogs.Clear();
            foreach (var log in logs)
            {
                AuditLogs.Add(log);
            }
            
            // Log that the log viewer was opened
            _auditLoggingService.LogAudit(
                "LOG_VIEWER_OPENED",
                null,
                "Log viewer opened by user"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogViewerViewModel] Error loading audit logs: {ex.Message}");
        }
    }

    private void OnLockStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        IsClearLogsEnabled = !e.IsLocked;
    }

    /// <summary>
    /// Refreshes the log display by reloading logs from file.
    /// </summary>
    [RelayCommand]
    private async Task RefreshLogs()
    {
        await LoadAuditLogsAsync();
    }

    /// <summary>
    /// Clears all logs from the display (does not delete the log file).
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        AuditLogs.Clear();
        _auditLoggingService.LogAudit(
            "LOGS_CLEARED",
            null,
            "Log viewer display cleared by user"
        );
    }
}
