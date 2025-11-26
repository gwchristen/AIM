using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<SearchViewModel>();
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is FileItem fileItem)
        {
            ViewModel.PreviewFileCommand.Execute(fileItem);
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is FileItem fileItem)
        {
            ViewModel.OpenInBrowseCommand.Execute(fileItem);
        }
    }

    private void CopyPathButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is FileItem fileItem)
        {
            ViewModel.CopyFilePathCommand.Execute(fileItem);
        }
    }
}