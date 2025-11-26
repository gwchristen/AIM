using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace AIM;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }
    public IServiceProvider Services { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices();
        Ioc.Default.ConfigureServices(Services);

        MainWindow = new MainWindow();

        var themeService = Ioc.Default.GetRequiredService<ThemeService>();
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
        services.AddSingleton<DirectoryOperationService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IPrintService, PrintService>();
        services.AddSingleton<FormTemplateFactory>();
        services.AddSingleton<SecurityService>();
        services.AddSingleton<EncryptionService>();
        services.AddSingleton<AuditLoggingService>();
        services.AddSingleton<EncryptedSettingsService>();
        services.AddSingleton<ThemeService>();


        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<BrowseViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddSingleton<SearchViewModel>();
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
        services.AddSingleton<BrowsePage>();
        services.AddTransient<PreviewPage>();
        services.AddSingleton<SearchPage>();
        services.AddSingleton<ScansPage>();
        services.AddTransient<StatsPage>();
        services.AddTransient<InventoryArchivePage>(); // This will become a UserControl view
        services.AddTransient<InventoryViewerPage>();
        // REMOVED: services.AddTransient<InventoryAdminPage>();
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