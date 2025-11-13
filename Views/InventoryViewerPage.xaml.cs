using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace AIM.Views;

public sealed partial class InventoryViewerPage : Page
{
    public InventoryViewerViewModel ViewModel { get; }

    public InventoryViewerPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<InventoryViewerViewModel>();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string path)
        {
            // Asynchronously load the archive contents.
            await ViewModel.LoadArchiveAsync(path);
        }
    }
}