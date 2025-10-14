using AIM.Models;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
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

        InitializeComponent();
        ViewModel = Ioc.Default.GetService<MainViewModel>();
        BrowseFrame.Navigate(typeof(BrowsePage));
        SearchFrame.Navigate(typeof(SearchPage));
        PreviewFrame.Navigate(typeof(PreviewPage));
        SettingsFrame.Navigate(typeof(SettingsPage));
    }

    private async void SelectRootButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            await ViewModel.LoadRootDirectoryAsync(folder.Path);
        }
    }

    private void DirectoryTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.FirstOrDefault() is DirectoryItem selected)
        {
            ViewModel.SelectedRootDirectory = selected;
            // Load files in Browse tab
            if (BrowseFrame.Content is BrowsePage browsePage)
            {
                browsePage.ViewModel.LoadFilesAsync(selected.FullPath);
            }
        }
    }

    private void DirectoryTree_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private void DirectoryTree_Drop(object sender, DragEventArgs e)
    {
        ViewModel.HandleFileDrop(e.DataView);
    }
}