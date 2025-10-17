using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;

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
        ViewModel.ShipRequested += OnShipRequested;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is DirectoryItem item)
        {
            ViewModel.UpdateLeftSelectedDirectory(item);
        }
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

    private async void OnShipRequested(FileItem file)
    {
        var dialog = new ContentDialog
        {
            Title = "Ship File",
            Content = "Move to shipped folder?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            XamlRoot = this.XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.CompleteShip();
        }
    }

    private void FilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Selection is bound
    }

    private void LeftLevel1_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.LeftLevel2.Clear();
        ViewModel.LeftLevel3.Clear();
        if (ViewModel.SelectedLeftLevel1 != null)
        {
            foreach (var sub in ViewModel.SelectedLeftLevel1.SubDirectories.Where(s => ViewModel.HasContents(s)))
            {
                ViewModel.LeftLevel2.Add(sub);
            }
        }
        ViewModel.SelectedLeftLevel2 = null;
        ViewModel.SelectedLeftLevel3 = null;
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void LeftLevel2_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.LeftLevel3.Clear();
        if (ViewModel.SelectedLeftLevel2 != null)
        {
            foreach (var sub in ViewModel.SelectedLeftLevel2.SubDirectories.Where(s => ViewModel.HasContents(s)))
            {
                ViewModel.LeftLevel3.Add(sub);
            }
        }
        ViewModel.SelectedLeftLevel3 = null;
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void LeftLevel3_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void ClearLeftLevel1_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedLeftLevel1 = null;
        ViewModel.LeftLevel2.Clear();
        ViewModel.LeftLevel3.Clear();
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void ClearLeftLevel2_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedLeftLevel2 = null;
        ViewModel.LeftLevel3.Clear();
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void ClearLeftLevel3_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedLeftLevel3 = null;
        ViewModel.UpdateLeftSelectedDirectory();
    }

    private void RightLevel1_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.RightLevel2.Clear();
        ViewModel.RightLevel3.Clear();
        if (ViewModel.SelectedRightLevel1 != null)
        {
            foreach (var sub in ViewModel.SelectedRightLevel1.SubDirectories)
            {
                ViewModel.RightLevel2.Add(sub);
            }
        }
        ViewModel.SelectedRightLevel2 = null;
        ViewModel.SelectedRightLevel3 = null;
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void RightLevel2_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.RightLevel3.Clear();
        if (ViewModel.SelectedRightLevel2 != null)
        {
            foreach (var sub in ViewModel.SelectedRightLevel2.SubDirectories)
            {
                ViewModel.RightLevel3.Add(sub);
            }
        }
        ViewModel.SelectedRightLevel3 = null;
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void RightLevel3_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void ClearRightLevel1_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedRightLevel1 = null;
        ViewModel.RightLevel2.Clear();
        ViewModel.RightLevel3.Clear();
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void ClearRightLevel2_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedRightLevel2 = null;
        ViewModel.RightLevel3.Clear();
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void ClearRightLevel3_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedRightLevel3 = null;
        ViewModel.UpdateRightSelectedDirectory();
    }

    private void FilteredContents_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedContent?.IsFolder == true)
        {
            var item = new DirectoryItem { Name = ViewModel.SelectedContent.Name, FullPath = ViewModel.SelectedContent.FullPath };
            try
            {
                var subs = Directory.GetDirectories(item.FullPath).Select(d => new DirectoryItem { Name = Path.GetFileName(d), FullPath = d });
                foreach (var sub in subs)
                {
                    item.SubDirectories.Add(sub);
                }
            }
            catch { }
            ViewModel.UpdateRightSelectedDirectory(item);
        }
    }

    private void FilesDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var selectedFile = ViewModel.SelectedFile;
        if (selectedFile != null)
        {
            var mainWindow = MainWindow.Instance;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Navigate(typeof(PreviewPage), selectedFile);
            }
        }
    }
}