using AIM.Models;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT;

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
        
        // Apply Mica backdrop
        SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };
        
        MainFrame.Navigate(typeof(BrowsePage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            MainFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var selectedItem = args.SelectedItem as NavigationViewItem;
        if (selectedItem != null)
        {
            var tag = selectedItem.Tag?.ToString();
            switch (tag)
            {
                case "SelectRoot":
                    SelectCustomRoot();
                    // Don't navigate, keep current page
                    break;
                case "RefreshTree":
                    ViewModel.RefreshTreeCommand.Execute(null);
                    // Don't navigate, keep current page
                    break;
                case "Browse":
                    MainFrame.Navigate(typeof(BrowsePage));
                    IsBrowseSelected = true;
                    IsPreviewSelected = false;
                    IsSearchSelected = false;
                    IsScansSelected = false;
                    IsInvArchivesSelected = false;
                    IsStatsSelected = false;
                    IsSettingsSelected = false;
                    break;
                case "Preview":
                    MainFrame.Navigate(typeof(PreviewPage));
                    IsBrowseSelected = false;
                    IsPreviewSelected = true;
                    IsSearchSelected = false;
                    IsScansSelected = false;
                    IsInvArchivesSelected = false;
                    IsStatsSelected = false;
                    IsSettingsSelected = false;
                    break;
                case "Search":
                    MainFrame.Navigate(typeof(SearchPage));
                    IsBrowseSelected = false;
                    IsPreviewSelected = false;
                    IsSearchSelected = true;
                    IsScansSelected = false;
                    IsInvArchivesSelected = false;
                    IsStatsSelected = false;
                    IsSettingsSelected = false;
                    break;
                case "Scans":
                    MainFrame.Navigate(typeof(ScansPage));
                    IsBrowseSelected = false;
                    IsPreviewSelected = false;
                    IsSearchSelected = false;
                    IsScansSelected = true;
                    IsInvArchivesSelected = false;
                    IsStatsSelected = false;
                    IsSettingsSelected = false;
                    break;
                case "InvArchives":
                    MainFrame.Navigate(typeof(InvArchivesPage));
                    IsBrowseSelected = false;
                    IsPreviewSelected = false;
                    IsSearchSelected = false;
                    IsScansSelected = false;
                    IsInvArchivesSelected = true;
                    IsStatsSelected = false;
                    IsSettingsSelected = false;
                    break;
                case "Stats":
                    MainFrame.Navigate(typeof(StatsPage));
                    IsBrowseSelected = false;
                    IsPreviewSelected = false;
                    IsSearchSelected = false;
                    IsScansSelected = false;
                    IsInvArchivesSelected = false;
                    IsStatsSelected = true;
                    IsSettingsSelected = false;
                    break;
                case "Settings":
                    MainFrame.Navigate(typeof(SettingsPage));
                    IsBrowseSelected = false;
                    IsPreviewSelected = false;
                    IsSearchSelected = false;
                    IsScansSelected = false;
                    IsInvArchivesSelected = false;
                    IsStatsSelected = false;
                    IsSettingsSelected = true;
                    break;
            }
        }
    }

    private void SelectCustomRoot()
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
}