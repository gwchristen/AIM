using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System; // Required for Tuple

namespace AIM.Views;

public sealed partial class ScansPage : Page
{
    public ScansViewModel ViewModel { get; }
    private ScanTreeItem _contextMenuItem;

    public ScansPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<ScansViewModel>();
        this.Unloaded += (s, e) => ViewModel.DeactivatePage();
    }

    private void ItemGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement tappedGrid) return;
        _contextMenuItem = tappedGrid.DataContext as ScanTreeItem;
        if (_contextMenuItem == null) return;

        var flyout = FlyoutBase.GetAttachedFlyout(tappedGrid);
        flyout.ShowAt(tappedGrid, new FlyoutShowOptions { Position = e.GetPosition(tappedGrid) });
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem != null && _contextMenuItem.IsFolder) { ViewModel.OpenFolderCommand.Execute(_contextMenuItem); }
    }

    private void MenuPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem != null && !_contextMenuItem.IsFolder) { ViewModel.OpenFileCommand.Execute(_contextMenuItem); }
    }

    private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem != null) { ViewModel.CopyPathCommand.Execute(_contextMenuItem); }
    }

    private async void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem == null) return;
        var item = _contextMenuItem;
        var dialog = new ContentDialog
        {
            Title = $"Delete {(item.IsFolder ? "Folder" : "File")}",
            Content = $"Are you sure you want to permanently delete '{item.Name}'?\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot,
            RequestedTheme = this.ActualTheme
        };
        dialog.PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary) { ViewModel.DeleteCommand.Execute(item); }
    }

    private void MenuRename_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem != null)
        {
            _contextMenuItem.IsRenaming = true;
        }
    }

    private void RenameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus(FocusState.Programmatic);
            textBox.SelectAll();
        }
    }

    private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not ScanTreeItem item) return;
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.RenameCommand.Execute(new Tuple<ScanTreeItem, string>(item, textBox.Text));
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            item.IsRenaming = false;
        }
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is ScanTreeItem item && item.IsRenaming)
        {
            ViewModel.RenameCommand.Execute(new Tuple<ScanTreeItem, string>(item, textBox.Text));
        }
    }

    private void BreadcrumbButton_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is BreadcrumbItem b) ViewModel.NavigateBreadcrumbCommand.Execute(b); }
    private void ItemsListView_ItemClick(object sender, ItemClickEventArgs e) { ViewModel.NavigateToFolderCommand.Execute(e.ClickedItem); }
    private void ItemsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) { if (ItemsListView.SelectedItem is ScanTreeItem i) ViewModel.OpenFileCommand.Execute(i); }
}