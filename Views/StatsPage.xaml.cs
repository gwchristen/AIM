using AIM.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AIM.Views;

public sealed partial class StatsPage : Page
{
    public StatsViewModel ViewModel { get; }

    public StatsPage()
    {
        this.InitializeComponent();
        ViewModel = new StatsViewModel(MainWindow.Instance?.ViewModel ?? throw new InvalidOperationException("MainViewModel not available"));
        DataContext = ViewModel;
    }

    private void ListBox_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ProblematicFile file)
        {
            ViewModel.OpenFile(file);
        }
    }
}