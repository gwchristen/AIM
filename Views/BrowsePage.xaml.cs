using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class BrowsePage : Page
{
    // This ViewModel is now private. The XAML will not see it directly.
    private readonly BrowseViewModel ViewModel;

    public BrowsePage()
    {
        this.InitializeComponent();

        // Get the ViewModel from Dependency Injection.
        ViewModel = Ioc.Default.GetRequiredService<BrowseViewModel>();

        // THE FIX: Explicitly set the DataContext.
        // The entire page will now use this for data binding.
        this.DataContext = ViewModel;
    }
}