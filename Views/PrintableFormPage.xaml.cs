using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class PrintableFormPage : Page
{
    public PrintableFormViewModel ViewModel { get; }

    public PrintableFormPage()
    {
        this.InitializeComponent();
        // Get the ViewModel from the DI container and set it as the DataContext
        ViewModel = Ioc.Default.GetRequiredService<PrintableFormViewModel>();
        this.DataContext = ViewModel;
    }

    // This method is called by the NavigationService when the page is navigated to.
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Pass the navigation parameter (the form data) to the ViewModel.
        ViewModel.OnNavigatedTo(e.Parameter);
    }
}