using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AIM;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    //private readonly SecurityService _securityService;
    private MainViewModel _mainViewModel;
    private SecurityService _securityService;
    private readonly IRefreshService _refreshService;

    public MainWindow()
    {
        this.InitializeComponent();
        _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
        _securityService = Ioc.Default.GetRequiredService<SecurityService>();
        _refreshService = Ioc.Default.GetRequiredService<IRefreshService>();
        _navigationService.Initialize(ContentFrame);
        _navigationService.NavigationChanged += NavigationService_NavigationChanged;

        // Get MainViewModel
        _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();

        Debug.WriteLine($"[MainWindow] MainViewModel obtained.  IsInventoryTabVisible: {_mainViewModel.IsInventoryTabVisible}");

        // Subscribe to property changes IMMEDIATELY - before NavView_Loaded
        _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

        if (this.Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _mainViewModel;
        }

        Debug.WriteLine($"[MainWindow] Constructor complete.  Property subscription added.");
    }

    private void NavigationService_NavigationChanged(Type pageType)
    {
        Debug.WriteLine($"[MainWindow] NavigationChanged event: {pageType.Name}");

        // Update our tracking of current page type
        _currentPageType = pageType;

        // Convert pageType to tag and highlight sidebar
        string tag = pageType switch
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
            _ => null
        };

        if (tag != null)
        {
            HighlightSidebarItem(tag);
        }
    }

    private void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Debug.WriteLine($"[MainWindow] PropertyChanged event received: {e.PropertyName}");

        if (e.PropertyName == nameof(MainViewModel.IsInventoryTabVisible))
        {
            Debug.WriteLine($"[MainWindow] IsInventoryTabVisible changed to: {_mainViewModel.IsInventoryTabVisible}");
            UpdateInventoryItemVisibility();
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[MainWindow] NavView_Loaded called");

        // Set initial visibility
        UpdateInventoryItemVisibility();
        UpdateLockUnlockButtonState();

        var browseItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == "Browse");
        if (browseItem != null)
        {
            NavView.SelectedItem = browseItem;
            _navigationService.NavigateTo(typeof(BrowsePage));
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
        else
        {
            Debug.WriteLine($"[MainWindow] ERROR: Could not find Inventory NavigationViewItem");
        }
    }

    private void UpdateLockUnlockButtonState()
    {
        if (LockUnlockButton != null)
        {
            if (_securityService.IsFullyUnlocked)
            {
                LockUnlockButton.Content = "Lock";
                Debug.WriteLine($"[MainWindow] Lock button updated to 'Lock'");
            }
            else
            {
                LockUnlockButton.Content = "Unlock";
                Debug.WriteLine($"[MainWindow] Lock button updated to 'Unlock'");
            }
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            _navigationService.NavigateTo(typeof(SettingsPage));
        }
        else if (args.InvokedItemContainer?.Tag is string navItemTag && !string.IsNullOrEmpty(navItemTag))
        {
            if (navItemTag == "LockUnlock")
            {
                HandleLockUnlockClick();
            }
            else if (navItemTag == "RefreshTree")
            {
                HandleRefreshClick();
            }
            else
            {
                NavigateToPage(navItemTag);
            }
        }
    }

    private void HandleRefreshClick()
    {
        Debug.WriteLine($"[MainWindow] Refresh button clicked - broadcasting refresh request");

        // Show a brief notification
        var infoBarService = Ioc.Default.GetRequiredService<IInfoBarService>();
        infoBarService.Show("Refreshing", "Refreshing all application data...", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational, 2000);

        // Broadcast refresh to all subscribed ViewModels
        _refreshService.RequestRefresh();

        Debug.WriteLine($"[MainWindow] Refresh request broadcast complete");
    }

    private async void HandleLockUnlockClick()
    {
        if (_securityService.IsFullyUnlocked)
        {
            // User is unlocked, lock the session
            _securityService.LockSession();
            _mainViewModel.UpdateInventoryTabVisibility();
            UpdateLockUnlockButtonState();
            Debug.WriteLine($"[MainWindow] Session locked from sidebar button");
        }
        else
        {
            // User is locked, show PIN dialog
            var dialog = new ContentDialog
            {
                Title = "Unlock with PIN",
                PrimaryButtonText = "Unlock",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content?.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 12 };
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Enter PIN to unlock:",
                TextWrapping = TextWrapping.Wrap
            });

            var pinBox = new PasswordBox { Width = 300 };
            stackPanel.Children.Add(pinBox);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string pin = pinBox.Password;

                if (_securityService.ValidatePin(pin))
                {
                    _mainViewModel.UpdateInventoryTabVisibility();
                    UpdateLockUnlockButtonState();
                    Debug.WriteLine($"[MainWindow] Session unlocked from sidebar button");
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Invalid PIN",
                        Content = "The PIN you entered is incorrect.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content?.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    Debug.WriteLine($"[MainWindow] Failed unlock attempt from sidebar button");
                }
            }
        }
    }

    /// <summary>
    /// Public method to update inventory tab visibility. 
    /// Called by SettingsViewModel when security status changes.
    /// </summary>
    public void UpdateInventoryTabVisibility(bool shouldBeVisible)
    {
        var inventoryItem = NavView.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(i => i.Content?.ToString() == "Inventory");

        if (inventoryItem != null)
        {
            Visibility newVisibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
            inventoryItem.Visibility = newVisibility;

            Debug.WriteLine($"[MainWindow] PUBLIC METHOD: Inventory item visibility set to: {newVisibility} (shouldBeVisible: {shouldBeVisible})");
        }
        else
        {
            Debug.WriteLine($"[MainWindow] PUBLIC METHOD: ERROR: Could not find Inventory NavigationViewItem");
        }

        UpdateLockUnlockButtonState();
    }

    private Dictionary<Type, Page> _pageCache = new();
    private Type _currentPageType = null;

    private void NavigateToPage(string navItemTag)
    {
        Debug.WriteLine($"[MainWindow] NavigateToPage called with tag: {navItemTag}");

        Type pageType = navItemTag switch
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

        Debug.WriteLine($"[MainWindow] Resolved pageType: {pageType?.Name ?? "null"}");

        if (pageType != null && _currentPageType != pageType)
        {
            // Use the navigation service to properly maintain the back stack
            _navigationService.NavigateTo(pageType);
            _currentPageType = pageType;
            HighlightSidebarItem(navItemTag);
        }
        else if (pageType == null)
        {
            Debug.WriteLine($"[MainWindow] Navigation skipped - pageType is null");
        }
        else
        {
            Debug.WriteLine($"[MainWindow] Navigation skipped - already on {pageType.Name}");
        }
    }

    private void NavigateToPageByType(Type pageType)
    {
        // Helper method for programmatic navigation by page type
        string tag = pageType switch
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
            _ => null
        };

        if (tag != null)
        {
            NavigateToPage(tag);
        }
    }

    private void HighlightSidebarItem(string tag)
    {
        // Find and select the navigation item with the matching tag
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (item.Tag?.ToString() == tag)
            {
                NavView.SelectedItem = item;
                Debug.WriteLine($"[MainWindow] Sidebar item '{tag}' selected");
                return;
            }

            // Check subitems for nested items
            if (item.MenuItems.Count > 0)
            {
                foreach (var subItem in item.MenuItems.OfType<NavigationViewItem>())
                {
                    if (subItem.Tag?.ToString() == tag)
                    {
                        NavView.SelectedItem = subItem;
                        Debug.WriteLine($"[MainWindow] Sidebar subitem '{tag}' selected");
                        return;
                    }
                }
            }
        }
    }
}