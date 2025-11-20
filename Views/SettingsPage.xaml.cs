using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AIM.Views;

public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    public SettingsViewModel ViewModel { get; }

    private bool _isLocked = true;

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<SettingsViewModel>();
        DataContext = ViewModel;
    }

    // Directory Browse Handlers
    private async void BrowseRootDirectory_Click(object sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.DefaultRootDirectory = path);
    }

    private async void BrowseArchivePath_Click(object sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.ArchivePath = path);
    }

    private async void BrowseShippedDirectory_Click(object sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.ShippedDirectory = path);
    }

    private async void BrowseFileScansDirectory_Click(object sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.FileScansDirectory = path);
    }

    private async void BrowseInventoryArchiveDirectory_Click(object sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.InventoryArchiveDirectory = path);
    }

    private async void BrowseSecurityConfigPath_Click(object sender, RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.SecurityConfigPath = path);
    }

    private async void BrowseSecurityDatabasePath_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            // Suggest a default database file name
            ViewModel.SecurityDatabasePath = System.IO.Path.Combine(folder.Path, "security.db");
        }
    }

    // Generic Folder Browser
    private async Task BrowseFolderAsync(Action<string> setPath)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            setPath(folder.Path);
        }
    }

    // Button Click Handlers
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveSettingsCommand.Execute(null);
    }

    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsLocked)
        {
            IsLocked = true;
        }
    }

    private async void UnlockButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsLocked)
        {
            var dialog = new ContentDialog
            {
                Title = "Enter Password",
                Content = new PasswordBox(),
                PrimaryButtonText = "Unlock",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var password = ((PasswordBox)dialog.Content).Password;
                if (password == ViewModel.Password)
                {
                    IsLocked = false;
                }
            }
        }
    }

    //Theme Selection Changed Handler
    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedTheme)
        {
            ViewModel.ChangeThemeCommand.Execute(selectedTheme);
        }
    }
}