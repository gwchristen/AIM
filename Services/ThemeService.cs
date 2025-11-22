using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace AIM.Services;

/// <summary>
/// Defines the available application theme options.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Automatically follows the Windows system theme preference.
    /// </summary>
    FollowSystem,
    
    /// <summary>
    /// Forces the application to use the light theme.
    /// </summary>
    Light,
    
    /// <summary>
    /// Forces the application to use the dark theme.
    /// </summary>
    Dark,
    
    /// <summary>
    /// Forces the application to use the high contrast theme.
    /// </summary>
    HighContrast
}

/// <summary>
/// Service responsible for managing application theme and appearance settings.
/// Handles theme changes, accent color detection, and high contrast mode support.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private AppTheme _currentTheme = AppTheme.FollowSystem;
    private Color _accentColor;
    private bool _isHighContrast;

    /// <summary>
    /// Event raised when the application theme changes.
    /// </summary>
    public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

    /// <summary>
    /// Gets or sets the current application theme.
    /// Setting this property applies the theme immediately and raises the ThemeChanged event.
    /// </summary>
    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ApplyTheme();
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { Theme = value });
            }
        }
    }

    /// <summary>
    /// Gets the current Windows accent color.
    /// This color is detected from the system settings.
    /// </summary>
    public Color AccentColor
    {
        get => _accentColor;
        private set => _accentColor = value;
    }

    /// <summary>
    /// Gets whether high contrast mode is currently enabled.
    /// High contrast is detected by analyzing the difference between foreground and background colors.
    /// </summary>
    public bool IsHighContrast
    {
        get => _isHighContrast;
        private set => _isHighContrast = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service used to load and save theme preferences.</param>
    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Load saved theme preference
        var appSettings = _settingsService.LoadSettings();
        if (Enum.TryParse<AppTheme>(appSettings.Theme ?? "FollowSystem", out var theme))
        {
            _currentTheme = theme;
        }

        // Get Windows accent color
        RefreshAccentColor();

        // Check for high contrast mode
        DetectHighContrast();

        Debug.WriteLine($"[Theme] Initialized theme service - Current theme: {_currentTheme}");
    }

    /// <summary>
    /// Initializes and applies the theme on application startup.
    /// This method should be called once during application initialization to set the initial theme.
    /// </summary>
    public void InitializeTheme()
    {
        ApplyTheme();
        Debug.WriteLine($"[Theme] Theme initialized");
    }

    /// <summary>
    /// Applies the current theme to the application window.
    /// This method updates the visual appearance based on the CurrentTheme setting.
    /// </summary>
    private void ApplyTheme()
    {
        try
        {
            var window = App.MainWindow;
            if (window == null)
            {
                Debug.WriteLine($"[Theme] MainWindow not available yet");
                return;
            }

            var rootElement = window.Content as FrameworkElement;
            if (rootElement == null)
            {
                Debug.WriteLine($"[Theme] Root element not found");
                return;
            }

            ElementTheme elementTheme = ElementTheme.Default;

            switch (_currentTheme)
            {
                case AppTheme.Light:
                    elementTheme = ElementTheme.Light;
                    Debug.WriteLine($"[Theme] Applying Light theme");
                    break;

                case AppTheme.Dark:
                    elementTheme = ElementTheme.Dark;
                    Debug.WriteLine($"[Theme] Applying Dark theme");
                    break;

                case AppTheme.HighContrast:
                    elementTheme = ElementTheme.Dark; // High contrast uses dark base
                    Debug.WriteLine($"[Theme] Applying High Contrast theme");
                    break;

                case AppTheme.FollowSystem:
                default:
                    // Detect system theme setting
                    elementTheme = DetectSystemTheme();
                    Debug.WriteLine($"[Theme] Following system theme: {elementTheme}");
                    break;
            }

            rootElement.RequestedTheme = elementTheme;

            Debug.WriteLine($"[Theme] Theme applied successfully: {elementTheme} for mode {_currentTheme}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Theme] ERROR applying theme: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Detects the current Windows system theme by analyzing the background color luminance.
    /// </summary>
    /// <returns>The detected <see cref="ElementTheme"/> based on system settings.</returns>
    private ElementTheme DetectSystemTheme()
    {
        try
        {
            var settings = new UISettings();
            var backgroundColor = settings.GetColorValue(UIColorType.Background);

            // Calculate luminance to determine if background is light or dark
            // Formula: (0.299 * R + 0.587 * G + 0.114 * B)
            double luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255.0;

            Debug.WriteLine($"[Theme] System background color: R={backgroundColor.R}, G={backgroundColor.G}, B={backgroundColor.B}, Luminance={luminance:F2}");

            // If luminance is high (> 0.5), background is light, so use Light theme
            // If luminance is low (< 0.5), background is dark, so use Dark theme
            ElementTheme detectedTheme = luminance > 0.5 ? ElementTheme.Light : ElementTheme.Dark;

            Debug.WriteLine($"[Theme] Detected system theme: {detectedTheme}");
            return detectedTheme;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Theme] ERROR detecting system theme: {ex.Message}");
            // Default to Dark if detection fails
            return ElementTheme.Dark;
        }
    }

    /// <summary>
    /// Refreshes the Windows accent color from system settings.
    /// Call this method to update the AccentColor property with the current system accent color.
    /// </summary>
    public void RefreshAccentColor()
    {
        try
        {
            var settings = new UISettings();
            _accentColor = settings.GetColorValue(UIColorType.Accent);
            Debug.WriteLine($"[Theme] Accent color updated: R={_accentColor.R}, G={_accentColor.G}, B={_accentColor.B}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Theme] ERROR getting accent color: {ex.Message}");
            _accentColor = Color.FromArgb(255, 0, 120, 215); // Default Windows blue
        }
    }

    /// <summary>
    /// Detects if Windows high contrast mode is currently enabled.
    /// Detection is based on the color difference between foreground and background colors.
    /// A difference greater than 450 indicates high contrast mode.
    /// </summary>
    private void DetectHighContrast()
    {
        try
        {
            // Check if high contrast is detected by comparing foreground and background colors
            var settings = new UISettings();
            var foreground = settings.GetColorValue(UIColorType.Foreground);
            var background = settings.GetColorValue(UIColorType.Background);

            // Calculate color difference
            int difference = Math.Abs(foreground.R - background.R) +
                            Math.Abs(foreground.G - background.G) +
                            Math.Abs(foreground.B - background.B);

            // High contrast typically has a very high color difference (> 450)
            _isHighContrast = difference > 450;

            Debug.WriteLine($"[Theme] High contrast detection - Foreground: R={foreground.R},G={foreground.G},B={foreground.B}, Background: R={background.R},G={background.G},B={background.B}, Difference: {difference}, Detected: {_isHighContrast}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Theme] ERROR detecting high contrast: {ex.Message}");
            _isHighContrast = false;
        }
    }

    /// <summary>
    /// Saves the specified theme preference to application settings.
    /// The saved preference will be loaded on next application startup.
    /// </summary>
    /// <param name="theme">The theme to save as the user's preference.</param>
    public void SaveThemePreference(AppTheme theme)
    {
        try
        {
            var appSettings = _settingsService.LoadSettings();
            appSettings.Theme = theme.ToString();
            _settingsService.SaveSettings(appSettings);
            Debug.WriteLine($"[Theme] Theme preference saved: {theme}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Theme] ERROR saving theme preference: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets an array of all available theme options.
    /// </summary>
    /// <returns>An array containing all <see cref="AppTheme"/> values.</returns>
    public AppTheme[] GetAvailableThemes()
    {
        return new[] { AppTheme.FollowSystem, AppTheme.Light, AppTheme.Dark, AppTheme.HighContrast };
    }

    /// <summary>
    /// Gets a user-friendly display name for the specified theme.
    /// </summary>
    /// <param name="theme">The theme to get the display name for.</param>
    /// <returns>A localized, user-friendly name for the theme.</returns>
    public string GetThemeName(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.FollowSystem => "Follow Windows Theme",
            AppTheme.Light => "Light",
            AppTheme.Dark => "Dark",
            AppTheme.HighContrast => "High Contrast",
            _ => "Unknown"
        };
    }

    public void SetTheme(string themeName)
    {
        if (App.MainWindow?.Content is FrameworkElement root)
        {
            if (themeName.Equals("Dark", StringComparison.OrdinalIgnoreCase))
            {
                root.RequestedTheme = ElementTheme.Dark;
            }
            else if (themeName.Equals("Light", StringComparison.OrdinalIgnoreCase))
            {
                root.RequestedTheme = ElementTheme.Light;
            }
            else
            {
                root.RequestedTheme = ElementTheme.Default;
            }
        }
    }
}

/// <summary>
/// Event arguments for the ThemeChanged event.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the new theme that was applied.
    /// </summary>
    public AppTheme Theme { get; set; }
}