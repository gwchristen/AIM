using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml;
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
            // Switch to Preview tab and navigate
            var mainWindow = (Window.Current as MainWindow);
            if (mainWindow != null)
            {
                mainWindow.FeatureTabs.SelectedIndex = 2; // Preview tab index
                mainWindow.PreviewFrame.Navigate(typeof(PreviewPage), selectedFile);
            }
        }
    }
}