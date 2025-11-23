using AIM.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace AIM.Views;

public sealed partial class ScansPage : Page
{
    public ScansViewModel ViewModel { get; }
    private bool isUpdatingSelection = false;

    public ScansPage()
    {
        this.InitializeComponent();
        ViewModel = new ScansViewModel();
        ViewModel.SelectedDirectoryChanged += OnSelectedDirectoryChanged;
        ViewModel.SortingDone += UpdateSelection;
        DataContext = ViewModel;
    }

    private void OnSelectedDirectoryChanged()
    {
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        isUpdatingSelection = true;
        FilesListView.SelectedItems.Clear();
        foreach (var file in ViewModel.Files)
        {
            if (MainWindow.Instance.ViewModel.SelectedScanFiles.Any(sf => sf.FullPath == file.FullPath))
            {
                FilesListView.SelectedItems.Add(file);
            }
        }
        isUpdatingSelection = false;
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
        if (isUpdatingSelection) return;

        if (sender is ListView listView)
        {
            var currentSelected = listView.SelectedItems.Cast<Models.FileItem>().ToList();
            var currentFiles = ViewModel.Files;

            // Remove from global those in currentFiles that are not selected
            foreach (var file in currentFiles)
            {
                if (!currentSelected.Contains(file))
                {
                    var toRemove = MainWindow.Instance.ViewModel.SelectedScanFiles.FirstOrDefault(sf => sf.FullPath == file.FullPath);
                    if (toRemove != null) MainWindow.Instance.ViewModel.SelectedScanFiles.Remove(toRemove);
                }
            }

            // Add selected
            foreach (var file in currentSelected)
            {
                if (!MainWindow.Instance.ViewModel.SelectedScanFiles.Any(sf => sf.FullPath == file.FullPath))
                {
                    MainWindow.Instance.ViewModel.SelectedScanFiles.Add(file);
                }
            }
        }
    }
}