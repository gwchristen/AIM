using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class DirAnalysisPage : Page
{
    public DirAnalysisViewModel ViewModel { get; }

    public DirAnalysisPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<DirAnalysisViewModel>();
        this.DataContext = ViewModel;
    }
}