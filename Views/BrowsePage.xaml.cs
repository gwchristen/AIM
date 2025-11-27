using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace AIM.Views;

public sealed partial class BrowsePage : Page
{
    private readonly BrowseViewModel ViewModel;
    private ContentItem _rightClickedItem;
    private readonly SolidColorBrush _dropHighlightBrush = new(Color.FromArgb(100, 16, 124, 16));
    private readonly SolidColorBrush _transparentBrush = new(Colors.Transparent);

    public BrowsePage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<BrowseViewModel>();
        this.DataContext = ViewModel;
    }

    #region Breadcrumb Navigation
    private void LeftBreadcrumbButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is BreadcrumbItem b)
            ViewModel.NavigateLeftBreadcrumbCommand.Execute(b);
    }

    private void RightBreadcrumbButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is BreadcrumbItem b)
            ViewModel.NavigateRightBreadcrumbCommand.Execute(b);
    }
    #endregion

    #region DataGrid Events
    private void LeftDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
    {
        if (e.Column.Tag is string sortPath)
        {
            ViewModel.SortCommand.Execute(sortPath);
        }
    }

    private void LeftDataGrid_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        List<ContentItem> filesToDrag;

        if (ViewModel.PersistentSelectedPaths.Any())
        {
            filesToDrag = ViewModel.GetPersistentSelectedFiles().ToList();
        }
        else
        {
            filesToDrag = LeftDataGrid.SelectedItems.Cast<ContentItem>().Where(i => !i.IsFolder).ToList();
        }

        if (!filesToDrag.Any())
        {
            e.Cancel = true;
            return;
        }

        e.Data.SetText(string.Join("|", filesToDrag.Select(i => i.FullPath)));
        e.Data.RequestedOperation = DataPackageOperation.Move;
        e.DragUI.SetContentFromDataPackage();
    }

    private void LeftDataGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is ContentItem item && item.IsFolder)
            {
                ViewModel.SelectedLeftDirectory = new DirectoryItem { FullPath = item.FullPath, Name = item.Name };
            }
        }
    }

    private void LeftDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ContentItem item && !item.IsFolder)
        {
            if (ViewModel.NavigateToPreviewCommand.CanExecute(null))
            {
                ViewModel.NavigateToPreviewCommand.Execute(null);
            }
        }
    }

    private void LeftDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var dataGrid = sender as DataGrid;
        if (dataGrid == null) return;

        ViewModel.SelectedLeftItems.Clear();
        foreach (var item in dataGrid.SelectedItems)
        {
            ViewModel.SelectedLeftItems.Add(item);
        }
    }

    private void LeftDataGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ContentItem item)
        {
            _rightClickedItem = item;

            if (!item.IsFolder)
            {
                if (!LeftDataGrid.SelectedItems.Contains(item))
                {
                    LeftDataGrid.SelectedItem = item;
                }

                var flyout = Resources["SourceFileContextMenu"] as MenuFlyout;
                flyout?.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
                e.Handled = true;
            }
        }
    }
    #endregion

    #region Right ListView Events with Drop Highlighting
    private void RightListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.None;

        // Show drop highlight
        DropTargetBorder.BorderBrush = _dropHighlightBrush;
        DropTargetBorder.Background = new SolidColorBrush(Color.FromArgb(30, 16, 124, 16));

        if ((e.OriginalSource as FrameworkElement)?.DataContext is ContentItem targetItem && targetItem.IsFolder)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = $"Move to {targetItem.Name}";
            e.DragUIOverride.IsGlyphVisible = true;
        }
        else if (ViewModel.SelectedRightDirectory != null)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = $"Move to {ViewModel.SelectedRightDirectory.Name}";
            e.DragUIOverride.IsGlyphVisible = true;
        }

        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
    }

    private void RightListView_DragLeave(object sender, DragEventArgs e)
    {
        // Remove drop highlight
        DropTargetBorder.BorderBrush = _transparentBrush;
        DropTargetBorder.Background = _transparentBrush;
    }

    private async void RightListView_Drop(object sender, DragEventArgs e)
    {
        // Remove drop highlight
        DropTargetBorder.BorderBrush = _transparentBrush;
        DropTargetBorder.Background = _transparentBrush;

        string destinationFolderPath = null;

        if ((e.OriginalSource as FrameworkElement)?.DataContext is ContentItem targetFolder && targetFolder.IsFolder)
        {
            destinationFolderPath = targetFolder.FullPath;
        }
        else if (ViewModel.SelectedRightDirectory != null)
        {
            destinationFolderPath = ViewModel.SelectedRightDirectory.FullPath;
        }

        if (destinationFolderPath != null && e.DataView.Contains(StandardDataFormats.Text))
        {
            var deferral = e.GetDeferral();
            try
            {
                var pathsString = await e.DataView.GetTextAsync();
                var sourceFilePaths = pathsString.Split('|');
                var dropData = new Tuple<IEnumerable<string>, string>(sourceFilePaths, destinationFolderPath);
                await ViewModel.MoveFilesCommand.ExecuteAsync(dropData);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
    #endregion

    #region Context Menu Events
    private void Page_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.Handled) return;

        var position = e.GetPosition(LeftDataGrid);
        if (position.X >= 0 && position.X <= LeftDataGrid.ActualWidth &&
            position.Y >= 0 && position.Y <= LeftDataGrid.ActualHeight)
        {
            return;
        }

        var flyout = Resources["GeneralContextMenu"] as MenuFlyout;
        flyout?.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
        e.Handled = true;
    }

    private void ContextMenu_Rename_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.RenameFileCommand.CanExecute(null))
            ViewModel.RenameFileCommand.Execute(null);
    }

    private void ContextMenu_AddToSelection_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.AddToSelectionCommand.CanExecute(null))
            ViewModel.AddToSelectionCommand.Execute(null);
    }

    private void ContextMenu_Edit_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.NavigateToPreviewCommand.CanExecute(null))
            ViewModel.NavigateToPreviewCommand.Execute(null);
    }

    private void ContextMenu_Archive_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ArchiveFileCommand.CanExecute(null))
            ViewModel.ArchiveFileCommand.Execute(null);
    }

    private void ContextMenu_Shipped_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ShipFileCommand.CanExecute(null))
            ViewModel.ShipFileCommand.Execute(null);
    }

    private void ContextMenu_Move_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.MoveFileCommand.CanExecute(null))
            ViewModel.MoveFileCommand.Execute(null);
    }

    private void ContextMenu_ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ClearSelectionCommand.CanExecute(null))
            ViewModel.ClearSelectionCommand.Execute(null);
    }

    private void ContextMenu_CopyFromScans_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CopyFromScansCommand.CanExecute(null))
            ViewModel.CopyFromScansCommand.Execute(null);
    }

    private async void ContextMenu_CopyFilepath_Click(object sender, RoutedEventArgs e)
    {
        var paths = new List<string>();

        if (ViewModel.PersistentSelectedPaths.Any())
        {
            paths.AddRange(ViewModel.PersistentSelectedPaths);
        }
        else if (ViewModel.SelectedLeftItems.Any())
        {
            paths.AddRange(ViewModel.SelectedLeftItems.Cast<ContentItem>().Select(i => i.FullPath));
        }
        else if (ViewModel.SelectedLeftDirectory != null)
        {
            paths.Add(ViewModel.SelectedLeftDirectory.FullPath);
        }

        if (paths.Any())
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(string.Join(Environment.NewLine, paths));
            Clipboard.SetContent(dataPackage);
            ViewModel.SetOperationStatusPublic($"Copied {paths.Count} path(s) to clipboard");
        }
    }

    private void ContextMenu_Undo_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.UndoCommand.CanExecute(null))
            ViewModel.UndoCommand.Execute(null);
    }
    #endregion
}