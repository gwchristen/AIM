using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AIM;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }
    public IServiceProvider Services { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices();
        Ioc.Default.ConfigureServices(Services);

        MainWindow = new MainWindow();

        // Load and validate settings before initializing services
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        try
        {
            var settings = settingsService.LoadSettings();
            System.Diagnostics.Debug.WriteLine("[App] Settings loaded successfully");
        }
        catch (SettingsNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] FATAL: Settings not found - {ex.Message}");
            await ShowSettingsErrorDialog(
                "Settings Not Found",
                "AIM has not been properly installed or the settings file is missing.\n\n" +
                "Please run the AIM installer to initialize the application.\n\n" +
                $"Technical details: {ex.Message}",
                allowContinue: false
            );
            Environment.Exit(1);
            return;
        }
        catch (SettingsCorruptedException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] FATAL: Settings corrupted - {ex.Message}");
            await ShowSettingsErrorDialog(
                "Settings Corrupted",
                "The AIM settings file is corrupted or invalid.\n\n" +
                "Options:\n" +
                "1. Reinstall AIM to reset settings\n" +
                "2. Restore settings.json from backup\n" +
                "3. Contact support for assistance\n\n" +
                $"Settings path: {SettingsService.GetCanonicalSettingsPath()}\n\n" +
                $"Technical details: {ex.Message}",
                allowContinue: false
            );
            Environment.Exit(1);
            return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] FATAL: Unexpected error loading settings - {ex.Message}");
            await ShowSettingsErrorDialog(
                "Unexpected Error",
                "An unexpected error occurred while loading settings.\n\n" +
                $"Technical details: {ex.Message}\n\n" +
                "Please reinstall AIM or contact support.",
                allowContinue: false
            );
            Environment.Exit(1);
            return;
        }

        // Initialize SecurityService before showing the main window
        var securityService = Ioc.Default.GetRequiredService<SecurityService>();
        await securityService.InitializeAsync();

        var themeService = Ioc.Default.GetRequiredService<IThemeService>();
        themeService.InitializeTheme();

        MainWindow.Activate();

    }

    /// <summary>
    /// Shows a blocking error dialog for settings-related errors.
    /// </summary>
    private async Task ShowSettingsErrorDialog(string title, string message, bool allowContinue)
    {
        try
        {
            // Ensure main window is created for dialog context
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = allowContinue ? "Continue Anyway" : "Exit",
                PrimaryButtonText = "Open Settings Folder",
                XamlRoot = MainWindow.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                // Open settings folder in explorer
                var settingsPath = SettingsService.GetCanonicalSettingsPath();
                var settingsDir = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(settingsDir) && Directory.Exists(settingsDir))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = settingsDir,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Error showing settings error dialog: {ex.Message}");
            // Fall through to exit
        }
    }


    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IInfoBarService, InfoBarService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IDirectoryOperationService, DirectoryOperationService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IPrintService, PrintService>();
        services.AddSingleton<FormTemplateFactory>();
        services.AddSingleton<SecurityService>();
        services.AddSingleton<EncryptionService>();
        services.AddSingleton<AuditLoggingService>();
        services.AddSingleton<IEncryptedSettingsService, EncryptedSettingsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISearchStateService, SearchStateService>();
        services.AddSingleton<IBrowseStateService, BrowseStateService>();


        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<BrowseViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddSingleton<ScansViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<InventoryArchiveViewModel>(); // Reused for Dir Archiver
        services.AddTransient<InventoryViewerViewModel>();
        services.AddTransient<InventoryAdminViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PrintableFormViewModel>();
        services.AddTransient<DirClonerViewModel>();
        services.AddTransient<BatchRenamerViewModel>();
        services.AddTransient<DirAnalysisViewModel>();
        services.AddTransient<FormGeneratorViewModel>();
        services.AddTransient<LogViewerViewModel>();


        // Pages
        services.AddTransient<BrowsePage>();
        services.AddTransient<PreviewPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<ScansPage>();
        services.AddTransient<StatsPage>();
        services.AddTransient<InventoryArchivePage>(); // This will become a UserControl view
        services.AddTransient<InventoryViewerPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<PrintableFormPage>();
        services.AddTransient<InventoryAdminToolsPage>();
        services.AddTransient<DirAnalysisPage>();
        services.AddTransient<FormGeneratorPage>(); 
        services.AddTransient<LogViewerPage>();


        return services.BuildServiceProvider();

    }

    public static T GetService<T>() where T : class
    {
        if ((Current as App)?.Services.GetService(typeof(T)) is not T service)
        {
            throw new InvalidOperationException($"Cannot resolve service of type {typeof(T).Name}");
        }

        return service;
    }

}
