using AIM.Services;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace AIM;

public partial class App : Application
{
    public static Window MainWindow { get; private set; }
    public App()
    {
        Services = ConfigureServices();
        Ioc.Default.ConfigureServices(Services); // This line is crucial
        InitializeComponent();
    }

    public new static App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register Services (as before)
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // == THIS IS THE CHANGE ==
        // Register all your ViewModels. We use AddTransient because a new instance
        // should be created every time a page is navigated to.
        services.AddSingleton<MainViewModel>();

        services.AddTransient<BrowseViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<ScansViewModel>();
        services.AddTransient<InvArchivesViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<SettingsViewModel>();


        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Assign to the new public static property
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}