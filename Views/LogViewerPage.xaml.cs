using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views
{

    public sealed partial class LogViewerPage : Page
    {
        public LogViewerViewModel ViewModel { get; }

        public LogViewerPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<LogViewerViewModel>();
            this.DataContext = ViewModel;
        }
    }
}
