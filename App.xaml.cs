using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AIM
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            InitializeSerilog();
        }

        /// <summary>
        /// Initializes Serilog for audit logging.
        /// Logs are written to %LOCALAPPDATA%\AIM\Logs\audit.log with daily rolling.
        /// </summary>
        private void InitializeSerilog()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = Path.Combine(localAppData, "AIM", "Logs");
            Directory.CreateDirectory(logDirectory);
        
            var logFilePath = Path.Combine(logDirectory, "audit.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("AIM Application started - Audit logging initialized");
        }

        public static Window? MainWindow { get; private set; }
        public IServiceProvider Services { get; private set; }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Services = ConfigureServices();
            Ioc.Default.ConfigureServices(Services);

            MainWindow = new MainWindow();

            // Load settings
            var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
            try
            {
                var settings = settingsService.LoadSettings();
                System.Diagnostics.Debug.WriteLine("[App] Settings loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error loading settings: {ex.Message}");
                // Continue anyway - settings will be created with defaults
            }

            var themeService = Ioc.Default.GetRequiredService<IThemeService>();
            themeService.InitializeTheme();

            MainWindow.Activate();
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
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ISearchStateService, SearchStateService>();
            services.AddSingleton<IBrowseStateService, BrowseStateService>();
            services.AddSingleton<ILockService, LockService>();
            services.AddSingleton<IAuditLoggingService, AuditLoggingService>();


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
}
