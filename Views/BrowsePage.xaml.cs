using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace AIM.Views
{
    public sealed partial class BrowsePage : Page
    {
        private readonly BrowseViewModel ViewModel;

        public BrowsePage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<BrowseViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            // Save state when navigating away from Browse tab
            ViewModel.SaveCurrentBrowseState();
            System.Diagnostics.Debug.WriteLine("[BrowsePage] Navigated away - state saved");
        }

        private void LeftBreadcrumbButton_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is BreadcrumbItem b) ViewModel.NavigateLeftBreadcrumbCommand.Execute(b); }
        private void RightBreadcrumbButton_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is BreadcrumbItem b) ViewModel.NavigateRightBreadcrumbCommand.Execute(b); }

        private void LeftDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column.Tag is string sortPath)
            {
                ViewModel.SortCommand.Execute(sortPath);
            }
        }

        private void LeftDataGrid_DragStarting(UIElement sender, DragStartingEventArgs e)
        {
            var filesToDrag = ViewModel.SelectedLeftItems.Cast<ContentItem>().Where(i => !i.IsFolder).ToList();
            if (!filesToDrag.Any())
            {
                e.Cancel = true;
                return;
            }

            e.Data.SetText(string.Join("|", filesToDrag.Select(i => i.FullPath)));
            e.Data.RequestedOperation = DataPackageOperation.Move;

            var dragVisual = new ItemsStackPanel();
            dragVisual.Children.Add(new TextBlock { Text = $"{filesToDrag.Count} file(s)" });
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
            ViewModel.SelectedLeftItems.Clear();
            foreach (var item in (sender as DataGrid).SelectedItems)
            {
                ViewModel.SelectedLeftItems.Add(item);
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
            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                var text = await e.DataView.GetTextAsync();
                var filePaths = text.Split('|');

                string destinationPath = null;

                if ((e.OriginalSource as FrameworkElement)?.DataContext is ContentItem targetItem && targetItem.IsFolder)
                {
                    destinationPath = targetItem.FullPath;
                }
                else if (ViewModel.SelectedRightDirectory != null)
                {
                    destinationPath = ViewModel.SelectedRightDirectory.FullPath;
                }

                if (destinationPath != null)
                {
                    ViewModel.MoveFilesCommand.Execute(new Tuple<IEnumerable<string>, string>(filePaths, destinationPath));
                }
            }
        }

        private void RightListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListView)?.SelectedItem is ContentItem item && item.IsFolder)
            {
                ViewModel.SelectedRightDirectory = new DirectoryItem { FullPath = item.FullPath, Name = item.Name };
            }
        }
    }
}
