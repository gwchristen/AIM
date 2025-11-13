using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System; // Required for Type

namespace AIM;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly MainViewModel _mainViewModel;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "AIM";

        // Get services from the DI container
        _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
        _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();

        var infoBarService = Ioc.Default.GetRequiredService<IInfoBarService>();
        infoBarService.Initialize(AppInfoBar);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationService.Initialize(ContentFrame);
        // Navigate to the initial page
        _navigationService.NavigateTo(typeof(BrowsePage));
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        var tag = item.Tag?.ToString();
        Type pageType = null;

        // This switch statement maps your original tags to the correct pages.
        switch (tag)
        {
            case "Browse":
                pageType = typeof(BrowsePage);
                break;
            case "Preview":
                pageType = typeof(PreviewPage);
                break;
            case "Search":
                pageType = typeof(SearchPage);
                break;
            case "Scans":
                pageType = typeof(ScansPage);
                break;
            case "Settings":
                pageType = typeof(SettingsPage);
                break;

            // THE FIX: Uncommented the case for the "Stats" tag.
            case "Stats":
                pageType = typeof(StatsPage);
                break;

            // TODO: Add case for "InvArchives" when the page exists
            // case "InvArchives":
            //     pageType = typeof(InvArchivesPage);
            //     break;

            // These are commands, not pages, so we handle them directly.
            case "SelectRoot":
                // We will add the SelectRootDirectoryCommand to MainViewModel later
                // _mainViewModel.SelectRootDirectoryCommand.Execute(null);
                return; // Stop after executing the command
            case "RefreshTree":
                // The existing logic already rebuilds the tree when the root changes.
                // We can add a dedicated refresh command if needed.
                // _mainViewModel.RefreshTreeCommand.Execute(null);
                return; // Stop after executing the command
        }

        if (pageType != null)
        {
            _navigationService.NavigateTo(pageType);
        }
    }
}