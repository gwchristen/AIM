using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

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
            Debug.WriteLine("File selected: " + selectedFile.Name);
            var mainWindow = MainWindow.Instance;
            Debug.WriteLine("MainWindow: " + mainWindow);
            if (mainWindow != null)
            {
                mainWindow.FeatureTabs.SelectedIndex = 2;
                Debug.WriteLine("PreviewFrame: " + mainWindow.PreviewFrame);
                mainWindow.PreviewFrame.Navigate(typeof(PreviewPage), selectedFile);
                Debug.WriteLine("Navigated");
            }
        }
    }
}