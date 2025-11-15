using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class FormGeneratorPage : Page
{
    public FormGeneratorViewModel ViewModel { get; }

    public FormGeneratorPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<FormGeneratorViewModel>();
        this.DataContext = ViewModel;
    }
}