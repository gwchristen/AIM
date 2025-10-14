using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; set; }

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = new SearchViewModel();
    }

    private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileItem selectedFile)
        {
            var mainWindow = MainWindow.Instance;
            if (mainWindow != null)
            {
                mainWindow.FeatureTabs.SelectedIndex = 2; // Preview tab
                mainWindow.PreviewFrame.Navigate(typeof(PreviewPage), selectedFile);
            }
        }
    }
}