using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class PreviewPage : Page
{
    public PreviewViewModel ViewModel { get; }

    public PreviewPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<PreviewViewModel>();
        // Set DataContext so XAML bindings work
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // This is the updated logic:
        // Check if the navigation parameter is a FileItem
        if (e.Parameter is FileItem fileItem)
        {
            // Call the method on the ViewModel to load the file,
            // instead of trying to set a property.
            ViewModel.LoadFileContent(fileItem);
        }
    }
}