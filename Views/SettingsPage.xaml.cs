using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.Views;

public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    // Reverted to use MainViewModel, where the settings properties live
    public MainViewModel ViewModel { get; }

    private bool _isLocked = true; // Default to locked

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
        // Request the existing MainViewModel from the DI container
        ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        DataContext = ViewModel; // Set the DataContext
    }

    private async void BrowseRootDirectory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.DefaultRootDirectory = path);
    }

    private async void BrowseArchivePath_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.ArchivePath = path);
    }

    private async void BrowseShippedDirectory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.ShippedDirectory = path);
    }

    private async void BrowseFileScansDirectory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.FileScansDirectory = path);
    }

    private async void BrowseInventoryArchiveDirectory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await BrowseFolderAsync(path => ViewModel.InventoryArchiveDirectory = path);
    }

    private async Task BrowseFolderAsync(Action<string> setPath)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        // Use the newly exposed App.MainWindow property
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            setPath(folder.Path);
        }
    }

    private void LockButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!IsLocked)
        {
            IsLocked = true;
        }
    }

    private async void UnlockButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

    private void SaveButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Settings are saved automatically via bindings in your MainViewModel
    }
}