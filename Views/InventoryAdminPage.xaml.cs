using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class InventoryAdminPage : Page
{
    public InventoryAdminViewModel ViewModel { get; }

    public InventoryAdminPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<InventoryAdminViewModel>();
        DataContext = ViewModel;
    }
}