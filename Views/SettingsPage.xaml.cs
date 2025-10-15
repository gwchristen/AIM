using AIM.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; set; } = new();

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.IsUnlockedChanged += OnIsUnlockedChanged;
        UpdateReadOnly();
    }

    private void OnIsUnlockedChanged(bool isUnlocked)
    {
        UpdateReadOnly();
    }

    private void UpdateReadOnly()
    {
        bool isReadOnly = !ViewModel.IsUnlocked;
        ArchiveTextBox.IsReadOnly = isReadOnly;
        DefaultRootTextBox.IsReadOnly = isReadOnly;
        ShippedTextBox.IsReadOnly = isReadOnly;
        InventoryArchiveTextBox.IsReadOnly = isReadOnly;
    }

    private async void ChangePasswordClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ViewModel.MainViewModel.Password))
        {
            // Prompt for current password
            var currentDialog = new ContentDialog
            {
                Title = "Enter Current Password",
                Content = new PasswordBox { PlaceholderText = "Current password" },
                PrimaryButtonText = "Next",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var currentResult = await currentDialog.ShowAsync();
            if (currentResult == ContentDialogResult.Primary)
            {
                var currentEntered = ((PasswordBox)currentDialog.Content).Password;
                if (currentEntered == ViewModel.MainViewModel.Password)
                {
                    // Prompt for new password
                    var newDialog = new ContentDialog
                    {
                        Title = "Enter New Password",
                        Content = new PasswordBox { PlaceholderText = "New password" },
                        PrimaryButtonText = "Set",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.XamlRoot
                    };
                    var newResult = await newDialog.ShowAsync();
                    if (newResult == ContentDialogResult.Primary)
                    {
                        var newEntered = ((PasswordBox)newDialog.Content).Password;
                        ViewModel.MainViewModel.Password = newEntered;
                    }
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = "Incorrect current password",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
        else
        {
            // No current password, set new one
            var newDialog = new ContentDialog
            {
                Title = "Set Password",
                Content = new PasswordBox { PlaceholderText = "Enter password" },
                PrimaryButtonText = "Set",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var newResult = await newDialog.ShowAsync();
            if (newResult == ContentDialogResult.Primary)
            {
                var newEntered = ((PasswordBox)newDialog.Content).Password;
                ViewModel.MainViewModel.Password = newEntered;
            }
        }
    }

    private async void UnlockClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ViewModel.MainViewModel.Password))
        {
            var dialog = new ContentDialog
            {
                Title = "Enter Password to Unlock",
                Content = new PasswordBox { PlaceholderText = "Enter password" },
                PrimaryButtonText = "Unlock",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var enteredPassword = ((PasswordBox)dialog.Content).Password;
                if (enteredPassword == ViewModel.MainViewModel.Password)
                {
                    ViewModel.IsUnlocked = true;
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = "Incorrect password",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
        else
        {
            // No password set, unlock directly
            ViewModel.IsUnlocked = true;
        }
    }

    private void LockClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.IsUnlocked = false;
    }

    private async void BrowseArchiveClicked(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder != null)
        {
            ViewModel.MainViewModel.ArchivePath = folder.Path;
        }
    }

    private async void BrowseDefaultRootClicked(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder != null)
        {
            ViewModel.MainViewModel.DefaultRootDirectory = folder.Path;
        }
    }

    private async void BrowseShippedClicked(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder != null)
        {
            ViewModel.MainViewModel.ShippedDirectory = folder.Path;
        }
    }

    private async void BrowseFileScansClicked(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder != null)
        {
            ViewModel.MainViewModel.FileScansDirectory = folder.Path;
        }
    }

    private async void BrowseInventoryArchiveClicked(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder != null)
        {
            ViewModel.MainViewModel.InventoryArchiveDirectory = folder.Path;
        }
    }

    private async Task<Windows.Storage.StorageFolder> PickFolderAsync()
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        return await folderPicker.PickSingleFolderAsync();
    }
}