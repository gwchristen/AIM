using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace AIM.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }
    private SearchResultItem _rightClickedItem;

    public SearchPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<SearchViewModel>();

        // Subscribe to populate history flyout
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.SearchHistory))
            {
                PopulateHistoryFlyout();
            }
        };

        PopulateHistoryFlyout();
    }

    private void PopulateHistoryFlyout()
    {
        HistoryFlyout.Items.Clear();

        foreach (var item in ViewModel.SearchHistory.Take(10))
        {
            var menuItem = new MenuFlyoutItem
            {
                Text = item,
                Icon = new FontIcon { Glyph = "\uE81C" }
            };
            menuItem.Click += (s, e) =>
            {
                ViewModel.SearchQuery = item;
                if (ViewModel.SearchCommand.CanExecute(null))
                {
                    ViewModel.SearchCommand.Execute(null);
                }
            };
            HistoryFlyout.Items.Add(menuItem);
        }

        if (HistoryFlyout.Items.Count > 0)
        {
            HistoryFlyout.Items.Add(new MenuFlyoutSeparator());
            var clearItem = new MenuFlyoutItem
            {
                Text = "Clear History",
                Icon = new FontIcon { Glyph = "\uE894" }
            };
            clearItem.Click += (s, e) => ViewModel.ClearHistoryCommand.Execute(null);
            HistoryFlyout.Items.Add(clearItem);
        }
    }

    #region Keyboard Accelerators
    private void FocusSearchAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchTextBox.Focus(FocusState.Programmatic);
        SearchTextBox.SelectAll();
        args.Handled = true;
    }

    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.IsSearching)
        {
            ViewModel.CancelSearchCommand.Execute(null);
        }
        else if (ViewModel.HasResults)
        {
            ViewModel.ClearCommand.Execute(null);
        }
        args.Handled = true;
    }

    private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.SearchCommand.CanExecute(null))
        {
            ViewModel.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }
    #endregion

    #region Results Actions
    private void ResultsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is SearchResultItem item)
        {
            ViewModel.PreviewFileCommand.Execute(item);
        }
    }

    private void ResultsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is SearchResultItem item)
        {
            _rightClickedItem = item;

            if (!ResultsListView.SelectedItems.Contains(item))
            {
                ResultsListView.SelectedItem = item;
            }

            var flyout = Resources["ResultContextMenu"] as MenuFlyout;
            flyout?.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
            e.Handled = true;
        }
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SearchResultItem item)
        {
            ViewModel.PreviewFileCommand.Execute(item);
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SearchResultItem item)
        {
            ViewModel.OpenInBrowseCommand.Execute(item);
        }
    }

    private void CopyPathButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SearchResultItem item)
        {
            ViewModel.CopyFilePathCommand.Execute(item);
        }
    }
    #endregion

    #region Context Menu Handlers
    private void ContextMenu_Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.PreviewFileCommand.Execute(_rightClickedItem);
        }
    }

    private void ContextMenu_OpenInBrowse_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.OpenInBrowseCommand.Execute(_rightClickedItem);
        }
    }

    private void ContextMenu_OpenFileLocation_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.OpenFileLocationCommand.Execute(_rightClickedItem);
        }
    }

    private void ContextMenu_CopyFilePath_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.CopyFilePathCommand.Execute(_rightClickedItem);
        }
    }

    private void ContextMenu_CopyDirPath_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.CopyDirectoryPathCommand.Execute(_rightClickedItem);
        }
    }
    #endregion

    #region Sorting
    private bool _sortAscending = true;

    private void ToggleSortDirection_Click(object sender, RoutedEventArgs e)
    {
        _sortAscending = !_sortAscending;
        SortDirectionIcon.Glyph = _sortAscending ? "\uE74A" : "\uE74B";
        ViewModel.ToggleSortDirectionCommand.Execute(_sortAscending);
    }
    #endregion
}