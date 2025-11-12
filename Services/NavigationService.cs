using Microsoft.UI.Xaml.Controls;
using System;

namespace AIM.Services;

public class NavigationService : INavigationService
{
    private Frame _frame;

    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }

    public void NavigateTo(Type pageType, object parameter = null)
    {
        if (_frame != null && _frame.CurrentSourcePageType != pageType)
        {
            _frame.Navigate(pageType, parameter);
        }
    }
}