using AIM.Models;
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Windows.UI;

namespace AIM.Views;

/// <summary>
/// Page displaying statistical analysis of inventory files and OpCo data.
/// </summary>
public sealed partial class StatsPage : Page
{
    public StatsViewModel ViewModel { get; }

    public StatsPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<StatsViewModel>();
        this.DataContext = ViewModel;

        // Set the legend text color programmatically to ensure visibility in all themes
        var legendColor = (Color)App.Current.Resources["TextFillColorPrimary"];
        var skColor = new SKColor(legendColor.R, legendColor.G, legendColor.B, legendColor.A);
        var paint = new SolidColorPaint(skColor);

        FileChart.LegendTextPaint = paint;
        DeviceChart.LegendTextPaint = paint;
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