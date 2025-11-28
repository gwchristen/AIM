using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace AIM.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public event NavigationChangedHandler NavigationChanged;

    public void Initialize(Frame frame)
    {
        _frame = frame;
        _frame.Navigated += Frame_Navigated;
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        // Fire navigation changed event for ALL navigations (including GoBack)
        NavigationChanged?.Invoke(e.SourcePageType);
    }

    public void NavigateTo(Type pageType)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation frame has not been initialized.");
        }
        _frame.Navigate(pageType);
        // Event will be fired by Frame_Navigated
    }

    public void NavigateTo(Type pageType, object parameter)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation frame has not been initialized.");
        }
        _frame.Navigate(pageType, parameter);
        // Event will be fired by Frame_Navigated
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            _frame?.GoBack();
            // Event will be fired by Frame_Navigated
        }
    }
}