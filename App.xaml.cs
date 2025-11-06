using AIM.Services;
using AIM.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AIM;

public partial class App : Application
{
    public App()
    {
        Services = ConfigureServices();
        Ioc.Default.ConfigureServices(Services);
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public new static App Current => (App)Application.Current!;

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
}
