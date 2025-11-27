using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using Windows.System;

namespace AIM.Views;

public sealed partial class ScansPage : Page
{
    public ScansViewModel ViewModel { get; }
    private ScanTreeItem _rightClickedItem;

    public ScansPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<ScansViewModel>();
    }

    #region Keyboard Accelerators
    private void SelectAllAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.SelectAllCommand.Execute(null);
        args.Handled = true;
    }

    private void DeleteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var selectedItem = ItemsListView.SelectedItem as ScanTreeItem;
        if (selectedItem != null)
        {
            ViewModel.DeleteCommand.Execute(selectedItem);
        }
        args.Handled = true;
    }

    private void RenameAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var selectedItem = ItemsListView.SelectedItem as ScanTreeItem;
        if (selectedItem != null)
        {
            selectedItem.IsRenaming = true;
        }
        args.Handled = true;
    }

    private void EnterAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var selectedItem = ItemsListView.SelectedItem as ScanTreeItem;
        if (selectedItem != null)
        {
            if (selectedItem.IsFolder)
            {
                ViewModel.NavigateToFolderCommand.Execute(selectedItem);
            }
            else
            {
                ViewModel.OpenFileCommand.Execute(selectedItem);
            }
        }
        args.Handled = true;
    }

    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var renamingItem = ViewModel.CurrentItems.FirstOrDefault(i => i.IsRenaming);
        if (renamingItem != null)
        {
            renamingItem.IsRenaming = false;
        }
        else
        {
            ViewModel.ClearSelectionCommand.Execute(null);
        }
        args.Handled = true;
    }

    private void RefreshAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.RefreshCommand.Execute(null);
        args.Handled = true;
    }
    #endregion

    #region Navigation
    private void BreadcrumbButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is BreadcrumbItem item)
        {
            ViewModel.NavigateBreadcrumbCommand.Execute(item);
        }
    }

    /// <summary>
    /// Single click - opens folders only
    /// </summary>
    private void ItemsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ScanTreeItem item && item.IsFolder)
        {
            ViewModel.NavigateToFolderCommand.Execute(item);
        }
    }

    /// <summary>
    /// Double click - opens files in preview
    /// </summary>
    private void ItemsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ScanTreeItem item && !item.IsFolder)
        {
            ViewModel.OpenFileCommand.Execute(item);
        }
    }
    #endregion

    #region Context Menu
    private void ItemsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ScanTreeItem item)
        {
            _rightClickedItem = item;

            if (Resources["ItemContextMenu"] is MenuFlyout flyout)
            {
                foreach (var menuItem in flyout.Items)
                {
                    if (menuItem is MenuFlyoutItem mfi)
                    {
                        if (mfi.Text == "Open Folder")
                        {
                            mfi.Visibility = item.IsFolder ? Visibility.Visible : Visibility.Collapsed;
                        }
                        else if (mfi.Text == "Preview" || mfi.Text == "Add to Browse Selection")
                        {
                            mfi.Visibility = item.IsFolder ? Visibility.Collapsed : Visibility.Visible;
                        }
                    }
                }

                flyout.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
            }
            e.Handled = true;
        }
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem?.IsFolder == true)
        {
            ViewModel.NavigateToFolderCommand.Execute(_rightClickedItem);
        }
    }

    private void MenuPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null && !_rightClickedItem.IsFolder)
        {
            ViewModel.OpenFileCommand.Execute(_rightClickedItem);
        }
    }

    private void MenuAddToBrowse_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null && !_rightClickedItem.IsFolder)
        {
            ViewModel.AddToBrowseSelectionCommand.Execute(_rightClickedItem);
        }
    }

    private void MenuSelectAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllCommand.Execute(null);
    }

    private void MenuClearSelection_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearSelectionCommand.Execute(null);
    }

    private async void MenuOpenFileLocation_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            await ViewModel.OpenFileLocationAsync(_rightClickedItem);
        }
    }

    private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.CopyPathCommand.Execute(_rightClickedItem);
        }
    }

    private void MenuRename_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            _rightClickedItem.IsRenaming = true;
        }
    }

    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.DeleteCommand.Execute(_rightClickedItem);
        }
    }
    #endregion

    #region Rename Handling
    private void RenameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus(FocusState.Programmatic);

            var name = textBox.Text;
            var dotIndex = name.LastIndexOf('.');
            if (dotIndex > 0)
            {
                textBox.Select(0, dotIndex);
            }
            else
            {
                textBox.SelectAll();
            }
        }
    }

    private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is ScanTreeItem item)
        {
            if (e.Key == VirtualKey.Enter)
            {
                ViewModel.RenameCommand.Execute(new Tuple<ScanTreeItem, string>(item, textBox.Text));
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Escape)
            {
                item.IsRenaming = false;
                e.Handled = true;
            }
        }
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is ScanTreeItem item)
        {
            if (item.IsRenaming)
            {
                ViewModel.RenameCommand.Execute(new Tuple<ScanTreeItem, string>(item, textBox.Text));
            }
        }
    }
    #endregion
}