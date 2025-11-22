using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views.AdminTools
{

    public sealed partial class DirClonerView : UserControl
    {
        public DirClonerViewModel ViewModel { get; }

        public DirClonerView()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<DirClonerViewModel>();
            this.DataContext = ViewModel;
        }
    }
}
