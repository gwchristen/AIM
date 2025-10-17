using AIM.Models;
using AIM.ViewModels;
using AIM.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        Instance = this;
        ViewModel = new MainViewModel();
        InitializeComponent();
        MainFrame.Navigate(typeof(BrowsePage));
    }

    private void SelectCustomRootButton_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        folderPicker.PickSingleFolderAsync().AsTask().ContinueWith(t =>
        {
            if (t.Result != null)
            {
                ViewModel.SelectedRoot = t.Result.Path;
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(BrowsePage));
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(PreviewPage));
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(SearchPage));
    }

    private void ScansButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(ScansPage));
    }

    private void InvArchivesButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(InvArchivesPage));
    }

    private void StatsButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(StatsPage));
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(SettingsPage));
    }
}