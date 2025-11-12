using AIM.Models;
using AIM.ViewModels;
using AIM.Services;
using CommunityToolkit.Mvvm.DependencyInjection; // Added
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AIM.Views;

public sealed partial class StatsPage : Page
{
    public StatsViewModel ViewModel { get; }

    public StatsPage()
    {
        this.InitializeComponent();

        // The ONLY change is here:
        ViewModel = Ioc.Default.GetRequiredService<StatsViewModel>();

        // Your original DataContext assignment is preserved
        DataContext = ViewModel;
    }

    // All of your original event handler logic is preserved
    private void ListBox_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ProblematicFile file)
        {
            ViewModel.OpenFileCommand.Execute(file);
        }
    }
}