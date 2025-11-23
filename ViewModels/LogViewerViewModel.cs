using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinRT.Interop;

namespace AIM.ViewModels;

public partial class LogViewerViewModel : ObservableObject
{
    private readonly AuditLoggingService _auditLoggingService;

    [ObservableProperty]
    private ObservableCollection<AuditLogEntry> allLogs;

    [ObservableProperty]
    private ObservableCollection<AuditLogEntry> filteredLogs;

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private string selectedActionTypeFilter = "All";

    [ObservableProperty]
    private string selectedUserFilter = "All";

    [ObservableProperty]
    private ObservableCollection<string> availableActionTypes;

    [ObservableProperty]
    private ObservableCollection<string> availableUsers;

    [ObservableProperty]
    private int totalLogCount;

    [ObservableProperty]
    private string logStatsMessage;

    public LogViewerViewModel(AuditLoggingService auditLoggingService)
    {
        _auditLoggingService = auditLoggingService;
        AllLogs = new ObservableCollection<AuditLogEntry>();
        FilteredLogs = new ObservableCollection<AuditLogEntry>();
        AvailableActionTypes = new ObservableCollection<string>();
        AvailableUsers = new ObservableCollection<string>();

        // Load logs on initialization
        LoadLogsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        try
        {
            var logs = await _auditLoggingService.GetLogsAsync();

            AllLogs.Clear();
            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                AllLogs.Add(log);
            }

            // Extract unique action types and users
            UpdateFilters();

            // Update stats
            TotalLogCount = AllLogs.Count;
            UpdateLogStats();

            Debug.WriteLine($"[LogViewer] Loaded {AllLogs.Count} logs");

            // Apply current filters
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LogViewer] ERROR loading logs: {ex.Message}");
        }
    }

    partial void OnFilterTextChanged(string value) => ApplyFilters();

    partial void OnSelectedActionTypeFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedUserFilterChanged(string value) => ApplyFilters();

    private void UpdateFilters()
    {
        // Get unique action types
        var actionTypes = AllLogs
            .Select(l => l.ActionType)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        AvailableActionTypes.Clear();
        AvailableActionTypes.Add("All");
        foreach (var actionType in actionTypes)
        {
            AvailableActionTypes.Add(actionType);
        }

        // Get unique users
        var users = AllLogs
            .Select(l => l.UserId)
            .Distinct()
            .OrderBy(u => u)
            .ToList();

        AvailableUsers.Clear();
        AvailableUsers.Add("All");
        foreach (var user in users)
        {
            AvailableUsers.Add(user);
        }
    }

    private void ApplyFilters()
    {
        var filtered = AllLogs.AsEnumerable();

        // Filter by action type
        if (!string.IsNullOrEmpty(SelectedActionTypeFilter) && SelectedActionTypeFilter != "All")
        {
            filtered = filtered.Where(l => l.ActionType == SelectedActionTypeFilter);
        }

        // Filter by user
        if (!string.IsNullOrEmpty(SelectedUserFilter) && SelectedUserFilter != "All")
        {
            filtered = filtered.Where(l => l.UserId.Equals(SelectedUserFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by text search
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var searchText = FilterText.ToLower();
            filtered = filtered.Where(l =>
                l.Description.ToLower().Contains(searchText) ||
                l.TargetPath.ToLower().Contains(searchText) ||
                l.Details.ToLower().Contains(searchText)
            );
        }

        FilteredLogs.Clear();
        foreach (var log in filtered.OrderByDescending(l => l.Timestamp))
        {
            FilteredLogs.Add(log);
        }

        UpdateLogStats();
    }

    private void UpdateLogStats()
    {
        LogStatsMessage = $"Showing {FilteredLogs.Count} of {TotalLogCount} total logs";
    }

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        try
        {
            // Create file picker
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV File", new System.Collections.Generic.List<string> { ".csv" });
            savePicker.FileTypeChoices.Add("JSON File", new System.Collections.Generic.List<string> { ".json" });
            savePicker.SuggestedFileName = $"AIM_Audit_Log_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}";

            // Get the window handle and initialize the file picker
            var window = App.MainWindow;
            if (window != null)
            {
                IntPtr hwnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Determine format based on file extension
                if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    await _auditLoggingService.ExportToCSVAsync(file.Path);
                }
                else
                {
                    await _auditLoggingService.ExportToJsonAsync(file.Path, FilteredLogs.ToList());
                }

                Debug.WriteLine($"[LogViewer] Logs exported to: {file.Path}");

                // Show success message
                var dialog = new ContentDialog
                {
                    Title = "Export Successful",
                    Content = $"Logs exported to:\n{file.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = App.MainWindow?.Content?.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LogViewer] ERROR exporting logs: {ex.Message}\n{ex.StackTrace}");

            // Show error dialog
            var dialog = new ContentDialog
            {
                Title = "Export Failed",
                Content = $"Error exporting logs: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    private async Task ClearAllLogsAsync()
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "Clear All Logs",
            Content = "Are you sure you want to delete all audit logs? This action cannot be undone.",
            PrimaryButtonText = "Clear",
            CloseButtonText = "Cancel",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            await _auditLoggingService.ClearLogsAsync();
            await LoadLogsAsync();
            Debug.WriteLine($"[LogViewer] All logs cleared");
        }
    }
}