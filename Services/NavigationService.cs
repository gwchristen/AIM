using Microsoft.UI.Xaml.Controls;
using System;

namespace AIM.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void NavigateTo(Type pageType)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation frame has not been initialized.");
        }
        _frame.Navigate(pageType);
    }

    public void NavigateTo(Type pageType, object parameter)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation frame has not been initialized.");
        }
        _frame.Navigate(pageType, parameter);
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            _frame?.GoBack();
        }
    }
}