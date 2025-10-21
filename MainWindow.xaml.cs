using AIM.Models;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM;

[ObservableObject]
public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public MainViewModel ViewModel { get; }

    [ObservableProperty]
    private bool isBrowseSelected = true;

    [ObservableProperty]
    private bool isPreviewSelected;

    [ObservableProperty]
    private bool isSearchSelected;

    [ObservableProperty]
    private bool isScansSelected;

    [ObservableProperty]
    private bool isInvArchivesSelected;

    [ObservableProperty]
    private bool isStatsSelected;

    [ObservableProperty]
    private bool isSettingsSelected;

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
        IsBrowseSelected = true;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(PreviewPage));
        IsBrowseSelected = false;
        IsPreviewSelected = true;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(SearchPage));
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = true;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void ScansButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(ScansPage));
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = true;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void InvArchivesButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(InvArchivesPage));
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = true;
        IsStatsSelected = false;
        IsSettingsSelected = false;
    }

    private void StatsButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(StatsPage));
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = true;
        IsSettingsSelected = false;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(SettingsPage));
        IsBrowseSelected = false;
        IsPreviewSelected = false;
        IsSearchSelected = false;
        IsScansSelected = false;
        IsInvArchivesSelected = false;
        IsStatsSelected = false;
        IsSettingsSelected = true;
    }
}