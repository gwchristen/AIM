using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AIM.Views.AdminTools;

public sealed partial class DirArchiverView : UserControl
{
    public InventoryArchiveViewModel ViewModel { get; }
    private ArchiveItem _rightClickedItem;

    public DirArchiverView()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<InventoryArchiveViewModel>();
        this.DataContext = ViewModel;
        this.Loaded += (s, e) => ViewModel.LoadArchivedDirectoriesCommand.Execute(null);
    }

    private void ArchiveItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ArchiveItem item)
        {
            ViewModel.ViewArchivedFolderCommand.Execute(item.Name);
        }
    }

    private void ArchiveItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ArchiveItem item)
        {
            _rightClickedItem = item;
            var flyout = Resources["ArchiveContextMenu"] as MenuFlyout;
            flyout?.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
            e.Handled = true;
        }
    }

    private void ViewArchive_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ArchiveItem item)
        {
            ViewModel.ViewArchivedFolderCommand.Execute(item.Name);
        }
    }

    private void ContextMenu_View_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.ViewArchivedFolderCommand.Execute(_rightClickedItem.Name);
        }
    }

    private async void ContextMenu_OpenExplorer_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            await ViewModel.OpenInExplorerAsync(_rightClickedItem);
        }
    }

    private void ContextMenu_Rename_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.RenameArchiveCommand.Execute(_rightClickedItem);
        }
    }

    private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem != null)
        {
            ViewModel.DeleteArchiveCommand.Execute(_rightClickedItem);
        }
    }
}