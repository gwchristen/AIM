using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class StatsPage : Page
{
    public StatsViewModel ViewModel { get; }

    public StatsPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<StatsViewModel>();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.LoadStatsCommand.Execute(null);
    }

    private void ProblematicFilesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ProblematicFilesListView.SelectedItem is ProblematicFile selectedFile)
        {
            ViewModel.OpenFile(selectedFile);
        }
    }
}