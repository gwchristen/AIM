using AIM.Models;
using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class PreviewPage : Page
{
    public PreviewViewModel ViewModel { get; set; }

    public PreviewPage()
    {
        InitializeComponent();
        ViewModel = new PreviewViewModel();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is FileItem file)
        {
            ViewModel.LoadFileContent(file);
        }
    }
}