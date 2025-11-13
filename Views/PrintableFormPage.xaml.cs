using AIM.Models;
using AIM.Services;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class PrintableFormPage : Page
{
    public PrintableFormViewModel ViewModel { get; }

    public PrintableFormPage()
    {
        // THE FIX: Use Ioc.Default to get the ViewModel, matching the app's architecture.
        ViewModel = Ioc.Default.GetRequiredService<PrintableFormViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is PrintableForm formData)
        {
            ViewModel.FormData = formData;
        }
    }

    private async void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        // THE FIX: Use Ioc.Default to get the PrintService.
        var printService = Ioc.Default.GetRequiredService<IPrintService>();
        await printService.PrintAsync(PrintableContent, "AIM Inventory Form");
    }
}