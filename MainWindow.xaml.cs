using AIM.Services;
using AIM.Views;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Diagnostics;

namespace AIM;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private MainViewModel _mainViewModel;

    public MainWindow()
    {
        this.InitializeComponent();
        _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
        _navigationService.Initialize(ContentFrame);

        // Get MainViewModel
        _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();

        Debug.WriteLine($"[MainWindow] MainViewModel obtained. IsInventoryTabVisible: {_mainViewModel.IsInventoryTabVisible}");

        // Subscribe to property changes IMMEDIATELY - before NavView_Loaded
        _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

        if (this.Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _mainViewModel;
        }

        Debug.WriteLine($"[MainWindow] Constructor complete. Property subscription added.");
        
        // Check if first-time setup is needed
        CheckFirstTimeSetup();
    }

    /// <summary>
    /// Checks if the application requires first-time password setup and prompts the user if needed.
    /// </summary>
    private async void CheckFirstTimeSetup()
    {
        var securityService = Ioc.Default.GetRequiredService<SecurityService>();
        
        if (securityService.IsFirstTimeSetup)
        {
            Debug.WriteLine("[MainWindow] First-time setup required");
            await ShowFirstTimeSetupDialogAsync();
        }
    }

    /// <summary>
    /// Shows a dialog for first-time master password setup.
    /// </summary>
    private async System.Threading.Tasks.Task ShowFirstTimeSetupDialogAsync()
    {
        var securityService = Ioc.Default.GetRequiredService<SecurityService>();
        
        while (securityService.IsFirstTimeSetup)
        {
            var dialog = new ContentDialog
            {
                Title = "Welcome to AIM - Initial Setup",
                PrimaryButtonText = "Set Password",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 12 };
            
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Welcome! Before you can use AIM, you need to set a master password.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 12)
            });
            
            stackPanel.Children.Add(new TextBlock
            {
                Text = PasswordValidator.GetPasswordRequirementsMessage(),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 12)
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Master Password:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            var passwordBox = new PasswordBox { Width = 300 };
            stackPanel.Children.Add(passwordBox);

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Confirm Password:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            var confirmPasswordBox = new PasswordBox { Width = 300 };
            stackPanel.Children.Add(confirmPasswordBox);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string password = passwordBox.Password;
                string confirmPassword = confirmPasswordBox.Password;

                if (string.IsNullOrWhiteSpace(password))
                {
                    await ShowErrorDialogAsync("Validation Error", "Password is required");
                    continue;
                }

                if (password != confirmPassword)
                {
                    await ShowErrorDialogAsync("Validation Error", "Passwords do not match");
                    continue;
                }

                if (!PasswordValidator.ValidatePassword(password, out string errorMessage))
                {
                    await ShowErrorDialogAsync("Password Requirements Not Met", errorMessage);
                    continue;
                }

                try
                {
                    await securityService.SetInitialPasswordAsync(password);
                    await ShowSuccessDialogAsync("Success", "Master password set successfully! You can now use AIM.");
                    Debug.WriteLine("[MainWindow] Initial password set successfully");
                    break;
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("Error", $"Failed to set password: {ex.Message}");
                    Debug.WriteLine($"[MainWindow] Error setting initial password: {ex.Message}");
                }
            }
            else
            {
                // User cancelled - close application
                Debug.WriteLine("[MainWindow] User cancelled first-time setup");
                Application.Current.Exit();
                break;
            }
        }
    }

    /// <summary>
    /// Shows an error dialog with the specified title and message.
    /// </summary>
    private async System.Threading.Tasks.Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a success dialog with the specified title and message.
    /// </summary>
    private async System.Threading.Tasks.Task ShowSuccessDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
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

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            _navigationService.NavigateTo(typeof(SettingsPage));
        }
        else if (args.InvokedItemContainer?.Tag is string navItemTag && !string.IsNullOrEmpty(navItemTag))
        {
            NavigateToPage(navItemTag);
        }
    }

    private void NavigateToPage(string navItemTag)
    {
        Debug.WriteLine($"[MainWindow] NavigateToPage called with tag: {navItemTag}");

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

        Debug.WriteLine($"[MainWindow] Resolved pageType: {pageType?.Name ?? "null"}");

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            Debug.WriteLine($"[MainWindow] Navigating to {pageType.Name}");
            _navigationService.NavigateTo(pageType);
        }
        else
        {
            Debug.WriteLine($"[MainWindow] Navigation skipped - pageType is null or already on page");
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
    }
}