using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace AIM.Views;

public sealed partial class StatsPage : Page
{
    public StatsViewModel ViewModel { get; }
    private ProblematicFile _rightClickedFile;

    public StatsPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<StatsViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Auto-load stats if not already loaded
        if (!ViewModel.HasLoaded && !ViewModel.IsLoading)
        {
            ViewModel.LoadStatsCommand.Execute(null);
        }
    }

    private void ProblematicFilesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ProblematicFile file)
        {
            ViewModel.OpenFileCommand.Execute(file);
        }
    }

    private void ProblematicFilesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ProblematicFile file)
        {
            _rightClickedFile = file;

            if (Resources["FileContextMenu"] is MenuFlyout flyout)
            {
                flyout.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
            }
            e.Handled = true;
        }
    }

    private void ContextMenu_Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedFile != null)
        {
            ViewModel.OpenFileCommand.Execute(_rightClickedFile);
        }
    }

    private void ContextMenu_OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedFile != null)
        {
            ViewModel.OpenFileLocationCommand.Execute(_rightClickedFile);
        }
    }

    private void ContextMenu_CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_rightClickedFile != null)
        {
            ViewModel.CopyPathCommand.Execute(_rightClickedFile);
        }
    }
}