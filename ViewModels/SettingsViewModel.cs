using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace AIM.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IDialogService _dialogService;
        private readonly IThemeService _themeService;
        private readonly ILockService _lockService;
        private readonly IAuditLoggingService _auditLoggingService;
        private AppSettings _appSettings;

        // Directory Properties
        [ObservableProperty]
        private string defaultRootDirectory;

        [ObservableProperty]
        private string archivePath;

        [ObservableProperty]
        private string shippedDirectory;

        [ObservableProperty]
        private string fileScansDirectory;

        [ObservableProperty]
        private string inventoryArchiveDirectory;

        // Theme Properties
        [ObservableProperty]
        private string selectedTheme;

        // Control Enabled Properties
        [ObservableProperty]
        private bool areDirectoryControlsEnabled;

        // Audit Log Properties
        [ObservableProperty]
        private ObservableCollection<LogEntry> auditLogs = new();

        [ObservableProperty]
        private int logEntryCount;

        public SettingsViewModel(
            ISettingsService settingsService,
            IDialogService dialogService,
            IThemeService themeService,
            ILockService lockService,
            IAuditLoggingService auditLoggingService)
        {
            _settingsService = settingsService;
            _dialogService = dialogService;
            _themeService = themeService;
            _lockService = lockService;
            _auditLoggingService = auditLoggingService;

            // Subscribe to lock state changes
            _lockService.LockStateChanged += OnLockStateChanged;
            AreDirectoryControlsEnabled = !_lockService.IsLocked;

            LoadSettings();
        }

        private void OnLockStateChanged(object? sender, LockStateChangedEventArgs e)
        {
            AreDirectoryControlsEnabled = !e.IsLocked;
            Debug.WriteLine($"[SettingsViewModel] Directory controls enabled: {AreDirectoryControlsEnabled}");
        }

        private void LoadSettings()
        {
            try
            {
                _appSettings = _settingsService.LoadSettings();

                DefaultRootDirectory = _appSettings.DefaultRootDirectory;
                ArchivePath = _appSettings.ArchivePath;
                ShippedDirectory = _appSettings.ShippedDirectory;
                FileScansDirectory = _appSettings.FileScansDirectory;
                InventoryArchiveDirectory = _appSettings.InventoryArchiveDirectory;
                SelectedTheme = _appSettings.Theme;

                Debug.WriteLine("[SettingsViewModel] Settings loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsViewModel] Error loading settings: {ex.Message}");
            }

            // Load audit logs
            _ = LoadAuditLogsAsync();
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            try
            {
                _appSettings.DefaultRootDirectory = DefaultRootDirectory;
                _appSettings.ArchivePath = ArchivePath;
                _appSettings.ShippedDirectory = ShippedDirectory;
                _appSettings.FileScansDirectory = FileScansDirectory;
                _appSettings.InventoryArchiveDirectory = InventoryArchiveDirectory;
                _appSettings.Theme = SelectedTheme;

                _settingsService.SaveSettings(_appSettings);

                _auditLoggingService.LogAudit(
                    "SETTINGS_SAVED",
                    null,
                    "Application settings saved"
                );

                await _dialogService.ShowSuccessDialog("Success", "Settings saved successfully.");

                Debug.WriteLine("[SettingsViewModel] Settings saved successfully");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync("Error", $"Failed to save settings: {ex.Message}");
                Debug.WriteLine($"[SettingsViewModel] Error saving settings: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SelectDefaultRootDirectoryAsync()
        {
            var path = await _dialogService.PickFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                DefaultRootDirectory = path;
            }
        }

        [RelayCommand]
        private async Task SelectArchivePathAsync()
        {
            var path = await _dialogService.PickFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                ArchivePath = path;
            }
        }

        [RelayCommand]
        private async Task SelectShippedDirectoryAsync()
        {
            var path = await _dialogService.PickFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                ShippedDirectory = path;
            }
        }

        [RelayCommand]
        private async Task SelectFileScansDirectoryAsync()
        {
            var path = await _dialogService.PickFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                FileScansDirectory = path;
            }
        }

        [RelayCommand]
        private async Task SelectInventoryArchiveDirectoryAsync()
        {
            var path = await _dialogService.PickFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                InventoryArchiveDirectory = path;
            }
        }

        [RelayCommand]
        private async Task RefreshLogsAsync()
        {
            await LoadAuditLogsAsync();
        }

        [RelayCommand]
        private void ClearLogs()
        {
            AuditLogs.Clear();
            LogEntryCount = 0;

            _auditLoggingService.LogAudit(
                "AUDIT_LOGS_CLEARED",
                null,
                "Audit logs cleared by user from Settings page"
            );
        }

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
                LogEntryCount = AuditLogs.Count;

                Debug.WriteLine($"[SettingsViewModel] Loaded {AuditLogs.Count} audit log entries");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsViewModel] Error loading audit logs: {ex.Message}");
            }
        }
    }
}