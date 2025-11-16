using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinRT.Interop;

namespace AIM.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly SecurityService _securityService;
    private readonly AuditLoggingService _auditLoggingService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly MainViewModel _mainViewModel;
    private readonly ThemeService _themeService;
    private AppSettings _appSettings;

    // Directory Settings Properties
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

    [ObservableProperty]
    private string securityConfigPath;

    [ObservableProperty]
    private string password;

    // Security Properties
    [ObservableProperty]
    private bool isDirectoriesUnlocked;

    [ObservableProperty]
    private bool isMasterPasswordOverrideActive;

    [ObservableProperty]
    private ObservableCollection<string> authorizedUsersList;

    [ObservableProperty]
    private string currentUserId;

    [ObservableProperty]
    private bool masterPasswordChangeSuccess;

    [ObservableProperty]
    private bool masterPasswordChangeError;

    [ObservableProperty]
    private string masterPasswordErrorMessage;

    // Audit Log Properties
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

    [ObservableProperty]
    private bool isUserAuthorized;

    // Theme Properties
    [ObservableProperty]
    private string selectedThemeName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> availableThemes;

    [ObservableProperty]
    private string accentColorHex;

    [ObservableProperty]
    private bool isHighContrast;

    // Constructor
    public SettingsViewModel(
        ISettingsService settingsService,
        SecurityService securityService,
        AuditLoggingService auditLoggingService,
        MainViewModel mainViewModel,
        IDialogService dialogService,
        INavigationService navigationService,
        ThemeService themeService)
    {
        _settingsService = settingsService;
        _securityService = securityService;
        _auditLoggingService = auditLoggingService;
        _mainViewModel = mainViewModel;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _themeService = themeService;

        // Initialize collections
        AuthorizedUsersList = new ObservableCollection<string>();
        AllLogs = new ObservableCollection<AuditLogEntry>();
        FilteredLogs = new ObservableCollection<AuditLogEntry>();
        AvailableActionTypes = new ObservableCollection<string>();
        AvailableUsers = new ObservableCollection<string>();
        AvailableThemes = new ObservableCollection<string>();

        // Load settings and check authorization
        LoadSettings();

        RefreshAuthorizedUsersList();
        UpdateUnlockStatus();

        CurrentUserId = _securityService.CurrentUserId;
        IsUserAuthorized = _securityService.IsFullyUnlocked;

        // Load theme settings
        InitializeThemes();

        Debug.WriteLine($"[Settings] Current user: {CurrentUserId}");
        Debug.WriteLine($"[Settings] Is authorized: {_securityService.IsFullyUnlocked}");

        // Load audit logs
        LoadLogsAsync().ConfigureAwait(false);
    }

    private void LoadSettings()
    {
        _appSettings = _settingsService.LoadSettings();

        DefaultRootDirectory = _appSettings.DefaultRootDirectory;
        ArchivePath = _appSettings.ArchivePath;
        ShippedDirectory = _appSettings.ShippedDirectory;
        FileScansDirectory = _appSettings.FileScansDirectory;
        InventoryArchiveDirectory = _appSettings.InventoryArchiveDirectory;
        SecurityConfigPath = _appSettings.SecurityConfigPath;
        Password = _appSettings.Password;
    }

    private void InitializeThemes()
    {
        AvailableThemes.Clear();
        foreach (var theme in _themeService.GetAvailableThemes())
        {
            AvailableThemes.Add(_themeService.GetThemeName(theme));
        }

        // Set the current theme name as a string
        SelectedThemeName = _themeService.GetThemeName(_themeService.CurrentTheme);
        UpdateAccentColorDisplay();
        IsHighContrast = _themeService.IsHighContrast;

        Debug.WriteLine($"[Settings] Initialized themes - Current: {SelectedThemeName}");
    }

    private void UpdateAccentColorDisplay()
    {
        var color = _themeService.AccentColor;
        AccentColorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        Debug.WriteLine($"[Settings] Accent color: {AccentColorHex}");
    }

    [RelayCommand]
    private void ChangeTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName))
            return;

        Debug.WriteLine($"[Settings] Theme selection changed to: {themeName}");

        // Convert theme name back to AppTheme enum
        AppTheme selectedTheme = themeName switch
        {
            "Follow Windows Theme" => AppTheme.FollowSystem,
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            "High Contrast" => AppTheme.HighContrast,
            _ => AppTheme.FollowSystem
        };

        _themeService.CurrentTheme = selectedTheme;
        _themeService.SaveThemePreference(selectedTheme);
        _themeService.InitializeTheme();

        LogAction("THEME_CHANGED", $"Application theme changed to {themeName}");
        Debug.WriteLine($"[Settings] Theme applied: {selectedTheme}");
    }

    [RelayCommand]
    private void RefreshAccentColor()
    {
        _themeService.RefreshAccentColor();
        UpdateAccentColorDisplay();

        LogAction("ACCENT_COLOR_REFRESHED", "Windows accent color was refreshed");
        Debug.WriteLine($"[Settings] Accent color refreshed");
    }

    private void RefreshAuthorizedUsersList()
    {
        AuthorizedUsersList.Clear();
        foreach (var user in _securityService.GetAuthorizedUsers())
        {
            AuthorizedUsersList.Add(user);
        }
        Debug.WriteLine($"[Settings] Refreshed authorized users list - Count: {AuthorizedUsersList.Count}");
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _appSettings.DefaultRootDirectory = DefaultRootDirectory;
        _appSettings.ArchivePath = ArchivePath;
        _appSettings.ShippedDirectory = ShippedDirectory;
        _appSettings.FileScansDirectory = FileScansDirectory;
        _appSettings.InventoryArchiveDirectory = InventoryArchiveDirectory;
        _appSettings.SecurityConfigPath = SecurityConfigPath;
        _appSettings.Password = Password;
        _appSettings.AuthorizedUsers = _securityService.GetAuthorizedUsers();

        _settingsService.SaveSettings(_appSettings);
        Debug.WriteLine($"[Settings] Settings saved");

        LogAction("SETTINGS_CHANGED", "Application settings were updated");
    }

    [RelayCommand(CanExecute = nameof(CanChangeMasterPassword))]
    private async Task ChangeMasterPasswordAsync()
    {
        if (!_securityService.IsFullyUnlocked)
        {
            await ShowErrorDialogAsync("Access Denied", "You must be authorized or have master override enabled to change the master password.");
            LogAction("MASTER_PASSWORD_CHANGE_DENIED", "Unauthorized user attempted to change master password");
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Change Master Password",
            PrimaryButtonText = "Change",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var stackPanel = new StackPanel { Spacing = 12 };

        stackPanel.Children.Add(new TextBlock
        {
            Text = "Current Master Password:",
            FontWeight = FontWeights.SemiBold
        });
        var currentPasswordBox = new PasswordBox { Width = 300 };
        stackPanel.Children.Add(currentPasswordBox);

        stackPanel.Children.Add(new TextBlock
        {
            Text = "New Master Password:",
            FontWeight = FontWeights.SemiBold
        });
        var newPasswordBox = new PasswordBox { Width = 300 };
        stackPanel.Children.Add(newPasswordBox);

        stackPanel.Children.Add(new TextBlock
        {
            Text = "Confirm New Password:",
            FontWeight = FontWeights.SemiBold
        });
        var confirmPasswordBox = new PasswordBox { Width = 300 };
        stackPanel.Children.Add(confirmPasswordBox);

        dialog.Content = stackPanel;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            string currentPassword = currentPasswordBox.Password;
            string newPassword = newPasswordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                await ShowErrorDialogAsync("Validation Error", "Current password is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                await ShowErrorDialogAsync("Validation Error", "New password is required");
                return;
            }

            if (newPassword != confirmPassword)
            {
                await ShowErrorDialogAsync("Validation Error", "New passwords do not match");
                return;
            }

            if (newPassword.Length < 6)
            {
                await ShowErrorDialogAsync("Validation Error", "New password must be at least 6 characters");
                return;
            }

            if (_securityService.ChangeMasterPassword(currentPassword, newPassword))
            {
                await ShowSuccessDialogAsync("Success", "Master password changed successfully!");
                LogAction("MASTER_PASSWORD_CHANGED", "User successfully changed the master password");
            }
            else
            {
                await ShowErrorDialogAsync("Error", "Current password is incorrect");
                LogAction("MASTER_PASSWORD_CHANGE_FAILED", "Failed to change master password - incorrect old password");
            }
        }
    }

    private bool CanChangeMasterPassword()
    {
        return _securityService.IsFullyUnlocked;
    }

    [RelayCommand]
    private async Task UnlockWithMasterPasswordAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Master Password Override",
            PrimaryButtonText = "Unlock",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var stackPanel = new StackPanel { Spacing = 12 };
        stackPanel.Children.Add(new TextBlock
        {
            Text = "Enter master password to unlock all features:",
            TextWrapping = TextWrapping.Wrap
        });

        var passwordBox = new PasswordBox { Width = 300 };
        stackPanel.Children.Add(passwordBox);

        dialog.Content = stackPanel;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            string password = passwordBox.Password;

            if (_securityService.ValidateMasterPassword(password))
            {
                IsMasterPasswordOverrideActive = _securityService.IsMasterPasswordOverrideActive;
                IsUserAuthorized = true;
                UpdateUnlockStatus();
                RefreshAuthorizedUsersList();

                _mainViewModel.UpdateInventoryTabVisibility();
                bool isNowVisible = _securityService.IsFullyUnlocked;
                UpdateMainWindowInventoryTab(isNowVisible);

                await ShowSuccessDialogAsync("Success", "Master password override activated. All features are now unlocked.");
                Debug.WriteLine($"[Settings] Master password override activated");
            }
            else
            {
                await ShowErrorDialogAsync("Invalid Password", "The password you entered is incorrect.");
                Debug.WriteLine($"[Settings] Master password override failed - incorrect password");
            }
        }
    }

    [RelayCommand]
    private void DeactivateMasterPasswordOverride()
    {
        Debug.WriteLine($"[Settings] DeactivateMasterPasswordOverride called");

        _securityService.DeactivateMasterPasswordOverride();
        IsMasterPasswordOverrideActive = false;
        IsUserAuthorized = false;
        UpdateUnlockStatus();

        _mainViewModel.UpdateInventoryTabVisibility();
        bool isNowVisible = _securityService.IsFullyUnlocked;
        UpdateMainWindowInventoryTab(isNowVisible);

        Debug.WriteLine($"[Settings] Master password override deactivated");

        LogAction("MASTER_LOCK", "Master password override was deactivated");
    }

    [RelayCommand]
    private void AddAuthorizedUser(string userId)
    {
        if (_securityService.IsFullyUnlocked && !string.IsNullOrWhiteSpace(userId))
        {
            _securityService.AddAuthorizedUser(userId);
            AuthorizedUsersList.Add(userId);
            SaveSettings();

            // Refresh authorization status
            IsUserAuthorized = _securityService.IsFullyUnlocked;
            UpdateUnlockStatus();

            UpdateMainWindowForUserChanges();

            Debug.WriteLine($"[Settings] Added authorized user: {userId}");
            LogAction("USER_ADDED", $"User '{userId}' was added to authorized users");
        }
    }

    [RelayCommand]
    private void RemoveAuthorizedUser(string userId)
    {
        if (_securityService.IsFullyUnlocked)
        {
            _securityService.RemoveAuthorizedUser(userId);
            AuthorizedUsersList.Remove(userId);
            SaveSettings();

            // Refresh authorization status
            IsUserAuthorized = _securityService.IsFullyUnlocked;
            UpdateUnlockStatus();

            UpdateMainWindowForUserChanges();

            Debug.WriteLine($"[Settings] Removed authorized user: {userId}");
            LogAction("USER_REMOVED", $"User '{userId}' was removed from authorized users");
        }
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

            UpdateFilters();
            TotalLogCount = AllLogs.Count;
            UpdateLogStats();

            Debug.WriteLine($"[Settings] Loaded {AllLogs.Count} logs");

            ApplyFilters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] ERROR loading logs: {ex.Message}");
        }
    }

    partial void OnFilterTextChanged(string value) => ApplyFilters();
    partial void OnSelectedActionTypeFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedUserFilterChanged(string value) => ApplyFilters();

    private void UpdateFilters()
    {
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

        if (!string.IsNullOrEmpty(SelectedActionTypeFilter) && SelectedActionTypeFilter != "All")
        {
            filtered = filtered.Where(l => l.ActionType == SelectedActionTypeFilter);
        }

        if (!string.IsNullOrEmpty(SelectedUserFilter) && SelectedUserFilter != "All")
        {
            filtered = filtered.Where(l => l.UserId.Equals(SelectedUserFilter, StringComparison.OrdinalIgnoreCase));
        }

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
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV File", new System.Collections.Generic.List<string> { ".csv" });
            savePicker.FileTypeChoices.Add("JSON File", new System.Collections.Generic.List<string> { ".json" });
            savePicker.SuggestedFileName = $"AIM_Audit_Log_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}";

            var window = App.MainWindow;
            if (window != null)
            {
                IntPtr hwnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    await _auditLoggingService.ExportToCSVAsync(file.Path);
                }
                else
                {
                    await _auditLoggingService.ExportToJsonAsync(file.Path, FilteredLogs.ToList());
                }

                Debug.WriteLine($"[Settings] Logs exported to: {file.Path}");

                await ShowSuccessDialogAsync("Export Successful", $"Logs exported to:\n{file.Path}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] ERROR exporting logs: {ex.Message}\n{ex.StackTrace}");
            await ShowErrorDialogAsync("Export Failed", $"Error exporting logs: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ClearAllLogsAsync()
    {
        if (!IsUserAuthorized)
        {
            await ShowErrorDialogAsync("Access Denied", "You do not have permission to clear audit logs. Only authorized users can clear logs.");
            _auditLoggingService.LogClearLogsAttempt(false, Environment.UserName);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Clear All Logs",
            Content = "Are you sure you want to delete all audit logs? This action cannot be undone.",
            PrimaryButtonText = "Clear",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                await _auditLoggingService.ClearLogsAsync(isAuthorized: true);
                _auditLoggingService.LogClearLogsAttempt(true, Environment.UserName);

                await LoadLogsAsync();

                await ShowSuccessDialogAsync("Logs Cleared", "All audit logs have been cleared.");
                Debug.WriteLine($"[Settings] Logs cleared by authorized user");
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error", $"Failed to clear logs: {ex.Message}");
            }
        }
    }

    private void UpdateUnlockStatus()
    {
        IsDirectoriesUnlocked = _securityService.IsFullyUnlocked;
        IsMasterPasswordOverrideActive = _securityService.IsMasterPasswordOverrideActive;

        ChangeMasterPasswordCommand.NotifyCanExecuteChanged();
        UnlockWithMasterPasswordCommand.NotifyCanExecuteChanged();
        SaveSettingsCommand.NotifyCanExecuteChanged();
        AddAuthorizedUserCommand.NotifyCanExecuteChanged();
        RemoveAuthorizedUserCommand.NotifyCanExecuteChanged();

        Debug.WriteLine($"[Settings] Unlock status updated - Directories unlocked: {IsDirectoriesUnlocked}");
        Debug.WriteLine($"[Settings] IsFullyUnlocked: {_securityService.IsFullyUnlocked}");
    }

    private void UpdateMainWindowInventoryTab(bool shouldBeVisible)
    {
        if (App.MainWindow is MainWindow mainWindow)
        {
            Debug.WriteLine($"[Settings] Directly updating MainWindow inventory tab visibility to: {shouldBeVisible}");
            mainWindow.UpdateInventoryTabVisibility(shouldBeVisible);
        }
        else
        {
            Debug.WriteLine($"[Settings] WARNING: Could not access MainWindow");
        }
    }

    private void UpdateMainWindowForUserChanges()
    {
        bool isNowVisible = _securityService.IsFullyUnlocked;
        UpdateMainWindowInventoryTab(isNowVisible);
    }

    private async Task ShowSuccessDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void LogAction(string actionType, string description)
    {
        var entry = new AuditLogEntry
        {
            ActionType = actionType,
            Description = description,
            UserId = CurrentUserId,
            TargetPath = "SETTINGS",
            Details = ""
        };

        _auditLoggingService.LogAction(entry);
    }
}