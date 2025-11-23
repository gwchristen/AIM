using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class InventoryArchivePage : Page
{
    public InventoryArchiveViewModel ViewModel { get; }

    public InventoryArchivePage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<InventoryArchiveViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Load the list of archived folders every time the page is viewed.
        ViewModel.LoadArchivedDirectoriesCommand.Execute(null);
    }

    private void ArchivedListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is string selectedFolder)
        {
            ViewModel.ViewArchivedFolderCommand.Execute(selectedFolder);
        }
    }
}