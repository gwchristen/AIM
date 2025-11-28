using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class InventoryViewerPage : Page
{
    public InventoryViewerViewModel ViewModel { get; }
    private ArchiveTreeNode _rightClickedNode;

    public InventoryViewerPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<InventoryViewerViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string path && !string.IsNullOrEmpty(path))
        {
            ViewModel.LoadArchiveCommand.Execute(path);
        }
    }

    private void ArchiveTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ArchiveTreeNode node)
        {
            _rightClickedNode = node;

            if (Resources["ItemContextMenu"] is MenuFlyout flyout)
            {
                // Show/hide preview option based on item type
                foreach (var item in flyout.Items)
                {
                    if (item is MenuFlyoutItem mfi && mfi.Text == "Preview File")
                    {
                        mfi.Visibility = node.IsFolder ? Visibility.Collapsed : Visibility.Visible;
                    }
                }

                flyout.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
            }
            e.Handled = true;
        }
    }

    private void ContextMenu_Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedNode != null && !_rightClickedNode.IsFolder)
        {
            ViewModel.PreviewFileCommand.Execute(_rightClickedNode);
        }
    }

    private async void ContextMenu_OpenExplorer_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedNode != null)
        {
            await ViewModel.OpenNodeInExplorerAsync(_rightClickedNode);
        }
    }

    private void ContextMenu_CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedNode != null)
        {
            ViewModel.CopyPathCommand.Execute(_rightClickedNode);
        }
    }
}