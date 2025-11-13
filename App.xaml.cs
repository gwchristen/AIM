using AIM.Services;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace AIM;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public static Window MainWindow { get; set; }

    public App()
    {
        this.InitializeComponent();

        Ioc.Default.ConfigureServices(
            new ServiceCollection()
                // Register services as Singletons
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<IFileService, FileService>()
                .AddSingleton<ISearchService, SearchService>()
                .AddSingleton<INavigationService, NavigationService>()
                .AddSingleton<IInfoBarService, InfoBarService>()
                .AddSingleton<IDialogService, DialogService>()

                // Register ViewModels
                .AddSingleton<MainViewModel>()
                .AddTransient<BrowseViewModel>()
                .AddTransient<ScansViewModel>()
                .AddTransient<SearchViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddTransient<PreviewViewModel>()
                // THE FIX: Register the StatsViewModel so the container knows how to create it.
                .AddTransient<StatsViewModel>()
                .BuildServiceProvider());
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}