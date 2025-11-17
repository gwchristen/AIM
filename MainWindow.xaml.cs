using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
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

    private async void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[MainWindow] NavView_Loaded called");

        // Check if first-time setup is needed
        if (_securityService.IsFirstTimeSetup)
        {
            Debug.WriteLine("[MainWindow] First-time setup detected, showing password setup dialog");
            await ShowFirstTimeSetupDialogAsync();
        }

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

    /// <summary>
    /// Shows the first-time master password setup dialog.
    /// This dialog blocks normal app usage until a valid password is set.
    /// </summary>
    private async Task ShowFirstTimeSetupDialogAsync()
    {
        while (_securityService.IsFirstTimeSetup)
        {
            var dialog = new ContentDialog
            {
                Title = "🔐 Set Master Password",
                PrimaryButtonText = "Set Password",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content?.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 12 };

            // Title
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Welcome to AIM",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 12)
            });

            // Instructions
            stackPanel.Children.Add(new TextBlock
            {
                Text = "First-time setup required. Please set a master password to secure your application.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 12)
            });

            // Password Requirements
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Password Requirements:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 8)
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = PasswordValidator.GetPasswordRequirementsMessage(),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 16)
            });

            // Master Password Input
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Master Password:",
                FontWeight = FontWeights.SemiBold
            });
            var passwordBox = new PasswordBox { Width = 300 };
            stackPanel.Children.Add(passwordBox);

            // Confirm Password Input
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Confirm Password:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0)
            });
            var confirmPasswordBox = new PasswordBox { Width = 300 };
            stackPanel.Children.Add(confirmPasswordBox);

            // Info bar for messages
            var infoBar = new InfoBar
            {
                IsOpen = false,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 16, 0, 0)
            };
            stackPanel.Children.Add(infoBar);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string password = passwordBox.Password;
                string confirmPassword = confirmPasswordBox.Password;

                // Validate inputs
                if (string.IsNullOrWhiteSpace(password))
                {
                    infoBar.Title = "Validation Error";
                    infoBar.Message = "Password cannot be empty.";
                    infoBar.Severity = InfoBarSeverity.Error;
                    infoBar.IsOpen = true;
                    continue;
                }

                if (password != confirmPassword)
                {
                    infoBar.Title = "Validation Error";
                    infoBar.Message = "Passwords do not match. Please try again.";
                    infoBar.Severity = InfoBarSeverity.Error;
                    infoBar.IsOpen = true;
                    continue;
                }

                // Validate password strength
                if (!PasswordValidator.ValidatePassword(password, out string errorMessage))
                {
                    infoBar.Title = "Password Requirements Not Met";
                    infoBar.Message = errorMessage;
                    infoBar.Severity = InfoBarSeverity.Error;
                    infoBar.IsOpen = true;
                    continue;
                }

                // Set the password
                try
                {
                    await _securityService.SetInitialPasswordAsync(password);
                    Debug.WriteLine("[MainWindow] Master password set successfully");
                    
                    // Show success message
                    var successDialog = new ContentDialog
                    {
                        Title = "✓ Success",
                        Content = new TextBlock
                        {
                            Text = "Master password has been set successfully! You can now use the application.",
                            TextWrapping = TextWrapping.Wrap
                        },
                        CloseButtonText = "OK",
                        XamlRoot = this.Content?.XamlRoot
                    };
                    await successDialog.ShowAsync();
                    break; // Exit the loop
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainWindow] ERROR setting master password: {ex.Message}");
                    infoBar.Title = "Error";
                    infoBar.Message = $"Failed to set password: {ex.Message}";
                    infoBar.Severity = InfoBarSeverity.Error;
                    infoBar.IsOpen = true;
                }
            }
            else
            {
                // User clicked Cancel - show warning
                var warningDialog = new ContentDialog
                {
                    Title = "Setup Required",
                    Content = new TextBlock
                    {
                        Text = "You must set a master password to use this application. Please try again.",
                        TextWrapping = TextWrapping.Wrap
                    },
                    CloseButtonText = "OK",
                    XamlRoot = this.Content?.XamlRoot
                };
                await warningDialog.ShowAsync();
            }
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
            _navigationService.NavigateTo(typeof(SettingsPage));
        }
        else if (args.InvokedItemContainer?.Tag is string navItemTag && !string.IsNullOrEmpty(navItemTag))
        {
            NavigateToPage(navItemTag);
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
            _navigationService.NavigateTo(pageType);
        }
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
