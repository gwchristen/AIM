using AIM.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class BrowsePage : Page
{
    public BrowseViewModel ViewModel { get; set; }

    public BrowsePage()
    {
        InitializeComponent();
        ViewModel = new BrowseViewModel();
    }
}