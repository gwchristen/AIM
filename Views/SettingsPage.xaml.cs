using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.Views;

public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    public MainViewModel ViewModel { get; }

    private bool _isLocked = false;

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
        ViewModel = MainWindow.Instance.ViewModel;
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
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            setPath(folder.Path);
        }
    }

    private async void LockButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
        // Settings are saved automatically via bindings
    }
}