using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AIM.Views;

public sealed partial class FormGeneratorPage : Page
{
    public FormGeneratorViewModel ViewModel { get; }

    public FormGeneratorPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<FormGeneratorViewModel>();
    }

    private void OhioTemplate_Click(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.SelectTemplateCommand.Execute("Ohio");
    }

    private void IMTemplate_Click(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.SelectTemplateCommand.Execute("I&M");
    }

    private void RecentDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is string path)
        {
            ViewModel.UseRecentDirectoryCommand.Execute(path);
        }
    }
}