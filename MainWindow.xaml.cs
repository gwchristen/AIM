using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private MainViewModel _mainViewModel;
    private SecurityService _securityService;

    public MainWindow()
    {
        this.InitializeComponent();
        _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
        _navigationService.Initialize(ContentFrame);

        // Subscribe to navigation requests to highlight the correct tab
        _navigationService.NavigationRequested += OnNavigationRequested;

        // Subscribe to frame navigation to update tab selection on back button
        ContentFrame.Navigated += ContentFrame_Navigated;

        // Get MainViewModel
        _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();

        // Get SecurityService
        _securityService = Ioc.Default.GetRequiredService<SecurityService>();

        Debug.WriteLine($"[MainWindow] MainViewModel obtained. IsInventoryTabVisible: {_mainViewModel.IsInventoryTabVisible}");

        // Subscribe to property changes IMMEDIATELY - before NavView_Loaded
        _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

        if (this.Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _mainViewModel;
        }

        Debug.WriteLine($"[MainWindow] Constructor complete. Property subscription added.");
    }

    private void OnNavigationRequested(string navigationTag)
    {
        Debug.WriteLine($"[MainWindow] Navigation requested for tag: {navigationTag}");
        SelectNavigationItem(navigationTag);
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        // Update the selected navigation item based on the current page type
        var currentPageType = ContentFrame.CurrentSourcePageType;
        var navigationTag = GetNavigationTagForPageType(currentPageType);

        if (!string.IsNullOrEmpty(navigationTag))
        {
            Debug.WriteLine($"[MainWindow] Frame navigated to: {currentPageType.Name}, selecting tag: {navigationTag}");
            SelectNavigationItem(navigationTag);
        }
    }

    private string? GetNavigationTagForPageType(Type pageType)
    {
        return pageType switch
        {
            _ when pageType == typeof(BrowsePage) => "Browse",
            _ when pageType == typeof(PreviewPage) => "Preview",
            _ when pageType == typeof(SearchPage) => "Search",
            _ when pageType == typeof(ScansPage) => "Scans",
            _ when pageType == typeof(InventoryAdminToolsPage) => "InventoryAdminTools",
            _ when pageType == typeof(InventoryViewerPage) => "InventoryViewer",
            _ when pageType == typeof(DirAnalysisPage) => "DirAnalysis",
            _ when pageType == typeof(FormGeneratorPage) => "PaperworkForms",
            _ when pageType == typeof(StatsPage) => "Stats",
            _ when pageType == typeof(LogViewerPage) => "LogViewer",
            _ when pageType == typeof(SettingsPage) => "Settings",
            _ => null
        };
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[MainWindow] NavView_Loaded called");

        UpdateInventoryItemVisibility();

        // Force rebuild of the directory tree to ensure it's populated
        if (!string.IsNullOrEmpty(_mainViewModel.SelectedRoot) && Directory.Exists(_mainViewModel.SelectedRoot))
        {
            Debug.WriteLine($"[MainWindow] Rebuilding tree for: {_mainViewModel.SelectedRoot}");
            // Trigger a re-initialization by setting SelectedRoot to itself
            var currentRoot = _mainViewModel.SelectedRoot;
            _mainViewModel.SelectedRoot = currentRoot;
        }

        var browseItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == "Browse");
        if (browseItem != null)
        {
            NavView.SelectedItem = browseItem;
            _navigationService.NavigateTo(typeof(BrowsePage));
        }
    }



    private void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsInventoryTabVisible))
        {
            UpdateInventoryItemVisibility();
        }
    }

    private void UpdateInventoryItemVisibility()
    {
        var inventoryItem = NavView.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(i => i.Content?.ToString() == "Inventory");

        if (inventoryItem != null)
        {
            bool shouldBeVisible = _mainViewModel.IsInventoryTabVisible;
            Visibility newVisibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
            inventoryItem.Visibility = newVisibility;

            Debug.WriteLine($"[MainWindow] Inventory item visibility set to: {newVisibility} (IsInventoryTabVisible: {shouldBeVisible})");
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            SelectNavigationItem("Settings");
            _navigationService.NavigateTo(typeof(SettingsPage));
        }
        else if (args.InvokedItemContainer?.Tag is string navItemTag && !string.IsNullOrEmpty(navItemTag))
        {
            if (navItemTag == "RefreshTree")
            {
                // Refresh the root directory tree
                if (!string.IsNullOrEmpty(_mainViewModel.SelectedRoot) && Directory.Exists(_mainViewModel.SelectedRoot))
                {
                    Debug.WriteLine($"[MainWindow] Refreshing directory tree for: {_mainViewModel.SelectedRoot}");
                    var currentRoot = _mainViewModel.SelectedRoot;
                    _mainViewModel.SelectedRoot = currentRoot;
                }
            }
            else
            {
                NavigateToPage(navItemTag);
            }
        }
    }

    private void NavigateToPage(string navItemTag)
    {
        Type? pageType = navItemTag switch
        {
            "RefreshTree" => null,
            "Browse" => typeof(BrowsePage),
            "Preview" => typeof(PreviewPage),
            "Search" => typeof(SearchPage),
            "Scans" => typeof(ScansPage),
            "InventoryAdminTools" => typeof(InventoryAdminToolsPage),
            "InventoryViewer" => typeof(InventoryViewerPage),
            "DirAnalysis" => typeof(DirAnalysisPage),
            "PaperworkForms" => typeof(FormGeneratorPage),
            "Stats" => typeof(StatsPage),
            "LogViewer" => typeof(LogViewerPage),
            _ => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            SelectNavigationItem(navItemTag);
            _navigationService.NavigateTo(pageType);
        }
    }

    private void SelectNavigationItem(string tag)
    {
        var item = FindNavigationItemByTag(NavView.MenuItems, tag);
        if (item != null)
        {
            NavView.SelectedItem = item;
            Debug.WriteLine($"[MainWindow] Selected navigation item: {tag}");
        }
        else if (tag == "Settings")
        {
            NavView.SelectedItem = NavView.SettingsItem;
            Debug.WriteLine($"[MainWindow] Selected settings item");
        }
    }

    private NavigationViewItem? FindNavigationItemByTag(IEnumerable<object> items, string tag)
    {
        foreach (var item in items)
        {
            if (item is NavigationViewItem navItem)
            {
                if (navItem.Tag?.ToString() == tag)
                {
                    return navItem;
                }

                // Check sub-items (for nested menu items like Inventory)
                if (navItem.MenuItems.Count > 0)
                {
                    var found = FindNavigationItemByTag(navItem.MenuItems, tag);
                    if (found != null) return found;
                }
            }
        }
        return null;
    }

    public void UpdateInventoryTabVisibility(bool shouldBeVisible)
    {
        var inventoryItem = NavView.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(i => i.Content?.ToString() == "Inventory");

        if (inventoryItem != null)
        {
            Visibility newVisibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
            inventoryItem.Visibility = newVisibility;
        }
    }
}