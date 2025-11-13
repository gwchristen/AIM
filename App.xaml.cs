using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
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
        // THE FIX: Register the new DirectoryOperationService
        services.AddSingleton<DirectoryOperationService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default); // Add Messenger
        services.AddSingleton<IPrintService, PrintService>(); // Register PrintService



        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<BrowseViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<ScansViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<InventoryArchiveViewModel>();
        services.AddTransient<InventoryViewerViewModel>();
        services.AddTransient<InventoryAdminViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PrintableFormViewModel>();


        // Pages
        services.AddTransient<BrowsePage>();
        services.AddTransient<PreviewPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<ScansPage>();
        services.AddTransient<StatsPage>();
        services.AddTransient<InventoryArchivePage>();
        services.AddTransient<InventoryViewerPage>();
        services.AddTransient<InventoryAdminPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<PrintableFormPage>();


        return services.BuildServiceProvider();
    }
}