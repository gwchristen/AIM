using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class InvArchivesPage : Page
{
    public InvArchivesViewModel ViewModel { get; }

    public InvArchivesPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<InvArchivesViewModel>();
    }
}