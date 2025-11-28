using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AIM.Views;

public sealed partial class DirAnalysisPage : Page
{
    public DirAnalysisViewModel ViewModel { get; }
    private FileAnomalyItem _rightClickedItem;

    public DirAnalysisPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<DirAnalysisViewModel>();
    }

    private void AnomalyItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FileAnomalyItem item)
        {
            ViewModel.OpenFileLocationCommand.Execute(item);
        }
    }

    private void AnomalyItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is FileAnomalyItem item)
        {
            _rightClickedItem = item;

            if (Resources["AnomalyContextMenu"] is MenuFlyout flyout)
            {
                flyout.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
            }
            e.Handled = true;
        }
    }

    private void ContextMenu_OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.OpenFileLocationCommand.Execute(_rightClickedItem);
        }
    }

    private void ContextMenu_Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.PreviewFileCommand.Execute(_rightClickedItem);
        }
    }

    private void ContextMenu_CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.CopyPathCommand.Execute(_rightClickedItem);
        }
    }
}