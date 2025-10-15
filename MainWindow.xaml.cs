using AIM.Models;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace AIM;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        ViewModel = Ioc.Default.GetService<MainViewModel>();
        BrowseFrame.Navigate(typeof(BrowsePage));
        SearchFrame.Navigate(typeof(SearchPage));
        SettingsFrame.Navigate(typeof(SettingsPage));
        // PreviewFrame navigated on demand
    }

    private void DirectoryTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs e)
    {
        if (e.InvokedItem is DirectoryItem item)
        {
            // Load files in BrowsePage
            if (BrowseFrame.Content is BrowsePage browsePage)
            {
                _ = browsePage.ViewModel.LoadFilesAsync(item);
            }
        }
    }

    private async void SelectCustomRootButton_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            ViewModel.SelectedRoot = folder.Path;
        }
    }

    private async void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ViewModel.SelectedRoot))
        {
            await ViewModel.LoadRootDirectoryAsync(ViewModel.SelectedRoot);
        }
    }

    // ... existing code ...
}