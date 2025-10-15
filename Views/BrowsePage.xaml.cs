using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace AIM.Views;

public sealed partial class BrowsePage : Page
{
    public BrowseViewModel ViewModel { get; set; }

    public BrowsePage()
    {
        InitializeComponent();
        ViewModel = new BrowseViewModel();
        ViewModel.RenameRequested += OnRenameRequested;
        ViewModel.DeleteRequested += OnDeleteRequested;
    }

    private async void OnRenameRequested(string fullPath, string currentName)
    {
        var dialog = new ContentDialog
        {
            Title = "Rename File",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            Content = new TextBox { Text = currentName },
            XamlRoot = this.XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var newName = ((TextBox)dialog.Content).Text;
            ViewModel.CompleteRename(newName);
        }
    }

    private async void OnDeleteRequested(FileItem file)
    {
        var dialog = new ContentDialog
        {
            Title = "Delete File",
            Content = "Move to archive?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            XamlRoot = this.XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.CompleteDelete();
        }
    }

    private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileItem selectedFile)
        {
            ViewModel.SelectedFile = selectedFile;
        }
    }

    private void FilesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var selectedFile = ViewModel.SelectedFile;
        if (selectedFile != null)
        {
            var mainWindow = MainWindow.Instance;
            if (mainWindow != null)
            {
                mainWindow.FeatureTabs.SelectedIndex = 2; // Preview tab
                mainWindow.PreviewFrame.Navigate(typeof(PreviewPage), selectedFile);
            }
        }
    }
}