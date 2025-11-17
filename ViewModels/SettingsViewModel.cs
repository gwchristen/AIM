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

/// <summary>
/// ViewModel for the Settings page, managing application configuration, security, and audit logs.
/// Provides properties and commands for directory settings, theme management, user authorization,
/// master password management, and audit log viewing/filtering.
/// </summary>
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
    
    /// <summary>
    /// Gets or sets the default root directory for file browsing operations.
    /// </summary>
    [ObservableProperty]
    private string defaultRootDirectory;

    /// <summary>
    /// Gets or sets the path where archived files are stored.
    /// </summary>
    [ObservableProperty]
    private string archivePath;

    /// <summary>
    /// Gets or sets the directory path for shipped items.
    /// </summary>
    [ObservableProperty]
    private string shippedDirectory;

    /// <summary>
    /// Gets or sets the directory where file scan results are stored.
    /// </summary>
    [ObservableProperty]
    private string fileScansDirectory;

    /// <summary>
    /// Gets or sets the directory where inventory archives are stored.
    /// </summary>
    [ObservableProperty]
    private string inventoryArchiveDirectory;

    /// <summary>
    /// Gets or sets the file path to the encrypted security configuration.
    /// </summary>
    [ObservableProperty]
    private string securityConfigPath;

    /// <summary>
    /// Gets or sets the application password.
    /// This property is deprecated; use SecurityConfigPath for encrypted password storage instead.
    /// </summary>
    [ObservableProperty]
    private string password;

    // Security Properties
    
    /// <summary>
    /// Gets or sets whether directory editing features are unlocked.
    /// </summary>
    [ObservableProperty]
    private bool isDirectoriesUnlocked;

    /// <summary>
    /// Gets or sets whether the master password override is currently active.
    /// </summary>
    [ObservableProperty]
    private bool isMasterPasswordOverrideActive;

    /// <summary>
    /// Gets or sets the collection of authorized user IDs.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> authorizedUsersList;

    /// <summary>
    /// Gets or sets the current user ID (Windows username).
    /// </summary>
    [ObservableProperty]
    private string currentUserId;

    /// <summary>
    /// Gets or sets whether the master password change operation succeeded.
    /// </summary>
    [ObservableProperty]
    private bool masterPasswordChangeSuccess;

    /// <summary>
    /// Gets or sets whether the master password change operation failed.
    /// </summary>
    [ObservableProperty]
    private bool masterPasswordChangeError;

    /// <summary>
    /// Gets or sets the error message for master password change failures.
    /// </summary>
    [ObservableProperty]
    private string masterPasswordErrorMessage;

    // Audit Log Properties
    
    /// <summary>
    /// Gets or sets the complete collection of all audit logs.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AuditLogEntry> allLogs;

    /// <summary>
    /// Gets or sets the filtered collection of audit logs based on current filter criteria.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AuditLogEntry> filteredLogs;

    /// <summary>
    /// Gets or sets the text filter applied to audit log search.
    /// </summary>
    [ObservableProperty]
    private string filterText = string.Empty;

    /// <summary>
    /// Gets or sets the selected action type filter for audit logs.
    /// </summary>
    [ObservableProperty]
    private string selectedActionTypeFilter = "All";

    /// <summary>
    /// Gets or sets the selected user filter for audit logs.
    /// </summary>
    [ObservableProperty]
    private string selectedUserFilter = "All";

    /// <summary>
    /// Gets or sets the collection of available action types for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> availableActionTypes;

    /// <summary>
    /// Gets or sets the collection of available users for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> availableUsers;

    /// <summary>
    /// Gets or sets the total count of audit logs before filtering.
    /// </summary>
    [ObservableProperty]
    private int totalLogCount;

    /// <summary>
    /// Gets or sets the message displaying log statistics (filtered vs. total).
    /// </summary>
    [ObservableProperty]
    private string logStatsMessage;

    /// <summary>
    /// Gets or sets whether the current user is authorized to access restricted features.
    /// </summary>
    [ObservableProperty]
    private bool isUserAuthorized;

    // Theme Properties
    
    /// <summary>
    /// Gets or sets the currently selected theme name for display in the UI.
    /// </summary>
    [ObservableProperty]
    private string selectedThemeName = string.Empty;

    /// <summary>
    /// Gets or sets the collection of available theme names.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> availableThemes;

    /// <summary>
    /// Gets or sets the hexadecimal representation of the Windows accent color.
    /// </summary>
    [ObservableProperty]
    private string accentColorHex;

    /// <summary>
    /// Gets or sets whether Windows high contrast mode is currently enabled.
    /// </summary>
    [ObservableProperty]
    private bool isHighContrast;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">Service for loading and saving application settings.</param>
    /// <param name="securityService">Service for managing security and authorization.</param>
    /// <param name="auditLoggingService">Service for managing audit logs.</param>
    /// <param name="mainViewModel">The main application view model.</param>
    /// <param name="dialogService">Service for displaying dialogs.</param>
    /// <param name="navigationService">Service for navigation between pages.</param>
    /// <param name="themeService">Service for managing application themes.</param>
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

    /// <summary>
    /// Loads application settings from storage and populates the view model properties.
    /// </summary>
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

    /// <summary>
    /// Initializes theme-related properties and loads available themes.
    /// </summary>
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

    /// <summary>
    /// Updates the accent color hex display string from the current theme service accent color.
    /// </summary>
    private void UpdateAccentColorDisplay()
    {
        var color = _themeService.AccentColor;
        AccentColorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        Debug.WriteLine($"[Settings] Accent color: {AccentColorHex}");
    }

    /// <summary>
    /// Command to change the application theme.
    /// </summary>
    /// <param name="themeName">The user-friendly name of the theme to apply.</param>
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

    /// <summary>
    /// Command to refresh the Windows accent color from system settings.
    /// </summary>
    [RelayCommand]
    private void RefreshAccentColor()
    {
        _themeService.RefreshAccentColor();
        UpdateAccentColorDisplay();

        LogAction("ACCENT_COLOR_REFRESHED", "Windows accent color was refreshed");
        Debug.WriteLine($"[Settings] Accent color refreshed");
    }

    /// <summary>
    /// Refreshes the authorized users list from the security service.
    /// </summary>
    private void RefreshAuthorizedUsersList()
    {
        AuthorizedUsersList.Clear();
        foreach (var user in _securityService.GetAuthorizedUsers())
        {
            AuthorizedUsersList.Add(user);
        }
        Debug.WriteLine($"[Settings] Refreshed authorized users list - Count: {AuthorizedUsersList.Count}");
    }

    /// <summary>
    /// Command to save all application settings.
    /// </summary>
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

    /// <summary>
    /// Command to change the master password after validating the current password.
    /// Prompts the user for current password, new password, and confirmation.
    /// Enforces strong password requirements.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

        // Show password requirements
        stackPanel.Children.Add(new TextBlock
        {
            Text = PasswordValidator.GetPasswordRequirementsMessage(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 12)
        });

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

            // Validate password strength
            if (!PasswordValidator.ValidatePassword(newPassword, out string errorMessage))
            {
                await ShowErrorDialogAsync("Password Requirements Not Met", errorMessage);
                return;
            }

            try
            {
                if (await _securityService.ChangeMasterPasswordAsync(currentPassword, newPassword))
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
            catch (ArgumentException ex)
            {
                await ShowErrorDialogAsync("Password Requirements Not Met", ex.Message);
                LogAction("MASTER_PASSWORD_CHANGE_FAILED", $"Password change rejected - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Determines whether the change master password command can execute.
    /// </summary>
    /// <returns><c>true</c> if the user is fully unlocked; otherwise, <c>false</c>.</returns>
    private bool CanChangeMasterPassword()
    {
        return _securityService.IsFullyUnlocked;
    }

    /// <summary>
    /// Command to unlock features using the master password.
    /// Prompts the user for the master password and activates override if valid.
    /// Implements rate limiting to prevent brute force attacks.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task UnlockWithMasterPasswordAsync()
    {
        // Check if locked out
        if (_securityService.IsLockedOut)
        {
            var remainingTime = _securityService.RemainingLockoutTime;
            await ShowErrorDialogAsync(
                "Authentication Locked", 
                $"Too many failed attempts. Please try again in {remainingTime?.TotalMinutes:F0} minutes."
            );
            return;
        }

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
                if (_securityService.IsLockedOut)
                {
                    var remainingTime = _securityService.RemainingLockoutTime;
                    await ShowErrorDialogAsync(
                        "Authentication Locked", 
                        $"Too many failed attempts. Authentication is locked for {remainingTime?.TotalMinutes:F0} minutes."
                    );
                }
                else
                {
                    await ShowErrorDialogAsync("Invalid Password", "The password you entered is incorrect.");
                }
                Debug.WriteLine($"[Settings] Master password override failed - incorrect password");
            }
        }
    }

    /// <summary>
    /// Command to deactivate the master password override.
    /// Locks features that require authorization.
    /// </summary>
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

    /// <summary>
    /// Command to add a user to the authorized users list.
    /// Requires the current user to be fully unlocked.
    /// </summary>
    /// <param name="userId">The user ID to add to the authorized list.</param>
    [RelayCommand]
    private async Task AddAuthorizedUserAsync(string userId)
    {
        if (_securityService.IsFullyUnlocked && !string.IsNullOrWhiteSpace(userId))
        {
            await _securityService.AddAuthorizedUserAsync(userId);
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

    /// <summary>
    /// Command to remove a user from the authorized users list.
    /// Requires the current user to be fully unlocked.
    /// </summary>
    /// <param name="userId">The user ID to remove from the authorized list.</param>
    [RelayCommand]
    private async Task RemoveAuthorizedUserAsync(string userId)
    {
        if (_securityService.IsFullyUnlocked)
        {
            await _securityService.RemoveAuthorizedUserAsync(userId);
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

    /// <summary>
    /// Command to load audit logs asynchronously from the audit logging service.
    /// Populates the AllLogs collection and applies current filters.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Partial method invoked when the filter text changes.
    /// Reapplies filters to the audit logs.
    /// </summary>
    partial void OnFilterTextChanged(string value) => ApplyFilters();
    
    /// <summary>
    /// Partial method invoked when the selected action type filter changes.
    /// Reapplies filters to the audit logs.
    /// </summary>
    partial void OnSelectedActionTypeFilterChanged(string value) => ApplyFilters();
    
    /// <summary>
    /// Partial method invoked when the selected user filter changes.
    /// Reapplies filters to the audit logs.
    /// </summary>
    partial void OnSelectedUserFilterChanged(string value) => ApplyFilters();

    /// <summary>
    /// Updates the available filter options based on the current audit logs.
    /// Populates action types and user lists for filtering.
    /// </summary>
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

    /// <summary>
    /// Applies the current filter criteria to the audit logs.
    /// Updates the FilteredLogs collection with matching entries.
    /// </summary>
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

    /// <summary>
    /// Updates the log statistics message showing filtered vs. total log counts.
    /// </summary>
    private void UpdateLogStats()
    {
        LogStatsMessage = $"Showing {FilteredLogs.Count} of {TotalLogCount} total logs";
    }

    /// <summary>
    /// Command to export audit logs to a file.
    /// Prompts the user to choose between CSV and JSON formats.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Command to clear all audit logs.
    /// Requires user authorization and prompts for confirmation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Updates the unlock status and notifies commands of state changes.
    /// </summary>
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

    /// <summary>
    /// Updates the inventory tab visibility in the main window.
    /// </summary>
    /// <param name="shouldBeVisible">Whether the inventory tab should be visible.</param>
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

    /// <summary>
    /// Updates the main window state when authorized users list changes.
    /// </summary>
    private void UpdateMainWindowForUserChanges()
    {
        bool isNowVisible = _securityService.IsFullyUnlocked;
        UpdateMainWindowInventoryTab(isNowVisible);
    }

    /// <summary>
    /// Shows a success dialog with the specified title and message.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The success message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Shows an error dialog with the specified title and message.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The error message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Logs an action to the audit logging service.
    /// </summary>
    /// <param name="actionType">The type of action being logged.</param>
    /// <param name="description">A description of the action.</param>
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