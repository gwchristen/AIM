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
                .AddSingleton<IDialogService, DialogService>() // The fix from the last step

                // Register ViewModels
                // THIS IS THE FIX: Simple, clean registration. The container will resolve its dependencies.
                .AddSingleton<MainViewModel>()
                .AddTransient<BrowseViewModel>()
                .AddTransient<ScansViewModel>()
                .AddTransient<SearchViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddTransient<PreviewViewModel>()
                .BuildServiceProvider());


    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}