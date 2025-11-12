using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection; // Added
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        this.InitializeComponent();

        // The ONLY change is here:
        ViewModel = Ioc.Default.GetRequiredService<SearchViewModel>();

        // Your original DataContext assignment is preserved:
        DataContext = ViewModel;
    }

    private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // All of your original logic is preserved:
        if (sender is ListView listView && listView.SelectedItem is Models.FileItem file)
        {
            // Navigate to Preview tab
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.MainFrame.Navigate(typeof(PreviewPage));
                // Set the selected tab
                MainWindow.Instance.IsPreviewSelected = true;
                MainWindow.Instance.IsBrowseSelected = false;
                MainWindow.Instance.IsSearchSelected = false;
                MainWindow.Instance.IsScansSelected = false;
                MainWindow.Instance.IsInvArchivesSelected = false;
                MainWindow.Instance.IsStatsSelected = false;
                MainWindow.Instance.IsSettingsSelected = false;

                // Load the file in Preview
                if (MainWindow.Instance.MainFrame.Content is PreviewPage previewPage)
                {
                    var fileItem = new FileItem
                    {
                        FullPath = file.FullPath,
                        Name = file.Name,
                        Type = file.Type
                    };
                    _ = previewPage.ViewModel.LoadFileContent(fileItem);
                }
            }
        }
    }
}