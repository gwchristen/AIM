using Microsoft.UI.Xaml;
using System;
using Windows.UI;

namespace AIM.Services;

/// <summary>
/// Interface for managing application theme and appearance settings.
/// Handles theme changes, accent color detection, and high contrast mode support.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Event raised when the application theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;

    /// <summary>
    /// Gets or sets the current application theme.
    /// Setting this property applies the theme immediately and raises the ThemeChanged event.
    /// </summary>
    AppTheme CurrentTheme { get; set; }

    /// <summary>
    /// Gets the current Windows accent color.
    /// This color is detected from the system settings.
    /// </summary>
    Color AccentColor { get; }

    /// <summary>
    /// Gets whether high contrast mode is currently enabled.
    /// High contrast is detected by analyzing the difference between foreground and background colors.
    /// </summary>
    bool IsHighContrast { get; }

    /// <summary>
    /// Initializes and applies the theme on application startup.
    /// This method should be called once during application initialization to set the initial theme.
    /// </summary>
    void InitializeTheme();

    /// <summary>
    /// Refreshes the Windows accent color from system settings.
    /// Call this method to update the AccentColor property with the current system accent color.
    /// </summary>
    void RefreshAccentColor();

    /// <summary>
    /// Saves the specified theme preference to application settings.
    /// The saved preference will be loaded on next application startup.
    /// </summary>
    /// <param name="theme">The theme to save as the user's preference.</param>
    void SaveThemePreference(AppTheme theme);

    /// <summary>
    /// Gets an array of all available theme options.
    /// </summary>
    /// <returns>An array containing all <see cref="AppTheme"/> values.</returns>
    AppTheme[] GetAvailableThemes();

    /// <summary>
    /// Gets a user-friendly display name for the specified theme.
    /// </summary>
    /// <param name="theme">The theme to get the display name for.</param>
    /// <returns>A localized, user-friendly name for the theme.</returns>
    string GetThemeName(AppTheme theme);
}
