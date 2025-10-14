using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class BrowsePage : Page
{
    public BrowseViewModel ViewModel { get; set; }

    public BrowsePage()
    {
        InitializeComponent();
        ViewModel = new BrowseViewModel();
    }

    private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileItem selectedFile)
        {
            var mainWindow = MainWindow.Instance;
            if (mainWindow != null)
            {
                mainWindow.FeatureTabs.SelectedIndex = 2;
                mainWindow.PreviewFrame.Navigate(typeof(PreviewPage), selectedFile);
            }
        }
    }
}