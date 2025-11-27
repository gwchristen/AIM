using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace AIM.Views;

public sealed partial class BrowsePage : Page
{
    private readonly BrowseViewModel ViewModel;

    public BrowsePage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<BrowseViewModel>();
        this.DataContext = ViewModel;
    }

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

    private void LeftDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
    {
        if (e.Column.Tag is string sortPath)
        {
            ViewModel.SortCommand.Execute(sortPath);
        }
    }

    private void LeftDataGrid_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        // Get files from persistent selection if any, otherwise from current DataGrid selection
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

        // Update ViewModel's SelectedLeftItems to match DataGrid
        ViewModel.SelectedLeftItems.Clear();
        foreach (var item in dataGrid.SelectedItems)
        {
            ViewModel.SelectedLeftItems.Add(item);
        }
    }

    /// <summary>
    /// Handle keyboard shortcuts for persistent selection
    /// </summary>
    private void LeftDataGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Ctrl+A to add current selection to persistent
        if (e.Key == VirtualKey.A &&
            Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            foreach (var item in LeftDataGrid.SelectedItems.Cast<ContentItem>())
            {
                if (!item.IsFolder)
                {
                    ViewModel.AddToPersistentSelection(item);
                }
            }
            e.Handled = true;
        }
    }

    private void RightListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.None;

        if ((e.OriginalSource as FrameworkElement)?.DataContext is ContentItem targetItem && targetItem.IsFolder)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = $"Move to {targetItem.Name}";
        }
        else if (ViewModel.SelectedRightDirectory != null)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = $"Move to {ViewModel.SelectedRightDirectory.Name}";
        }

        e.DragUIOverride.IsCaptionVisible = true;
    }

    private async void RightListView_Drop(object sender, DragEventArgs e)
    {
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
}