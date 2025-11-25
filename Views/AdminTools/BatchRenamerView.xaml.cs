using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views.AdminTools;

public sealed partial class BatchRenamerView : UserControl
{
    public BatchRenamerViewModel ViewModel { get; }

    public BatchRenamerView()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<BatchRenamerViewModel>();
        this.DataContext = ViewModel;
    }
}