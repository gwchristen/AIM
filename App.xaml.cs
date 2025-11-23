using AIM.Services;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace AIM;

public partial class App : Application
{
    public App()
    {
        Services = ConfigureServices();
        Ioc.Default.ConfigureServices(Services); // Add this line to configure the Ioc container
        InitializeComponent();
    }

    public new static App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        services.AddTransient<MainViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window m_window;
}