using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace AIM.Views
{

    public sealed partial class SearchPage : Page
    {
        public SearchViewModel ViewModel { get; }
        private FileItem? _contextMenuItem;

        public SearchPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<SearchViewModel>();
        }

        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No need to manage button state since they're in template now
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is FileItem fileItem)
            {
                ViewModel.PreviewCommand.Execute(fileItem);
            }
        }

        private void OpenInBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is FileItem fileItem)
            {
                ViewModel.OpenInBrowseCommand.Execute(fileItem);
            }
        }

        private void CopyPathButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is FileItem fileItem)
            {
                ViewModel.CopyFilePathCommand.Execute(fileItem);
            }
        }

        private void ResultsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is FileItem fileItem)
            {
                _contextMenuItem = fileItem;
                ResultsListView.SelectedItem = fileItem;
                var listView = sender as ListView;
                var flyout = FlyoutBase.GetAttachedFlyout(listView);
                if (flyout != null)
                {
                    flyout.ShowAt(listView, new FlyoutShowOptions { Position = e.GetPosition(listView) });
                }
            }
        }

        private void MenuPreview_Click(object sender, RoutedEventArgs e)
        {
            if (_contextMenuItem != null)
            {
                ViewModel.PreviewCommand.Execute(_contextMenuItem);
            }
        }

        private void MenuOpenInBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (_contextMenuItem != null)
            {
                ViewModel.OpenInBrowseCommand.Execute(_contextMenuItem);
            }
        }

        private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (_contextMenuItem != null)
            {
                ViewModel.CopyFilePathCommand.Execute(_contextMenuItem);
            }
        }
    }
}
