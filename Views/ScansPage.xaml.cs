using AIM.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace AIM.Views;

public sealed partial class ScansPage : Page
{
    public ScansViewModel ViewModel { get; }

    public ScansPage()
    {
        this.InitializeComponent();
        ViewModel = new ScansViewModel();
        DataContext = ViewModel;
    }

    private void FilesListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is Models.FileItem file)
        {
            ViewModel.OpenFile(file);
        }
    }

    private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            ViewModel.SelectedFiles = new ObservableCollection<Models.FileItem>(listView.SelectedItems.Cast<Models.FileItem>());
        }
    }
}