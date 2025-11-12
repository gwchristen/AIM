using Microsoft.UI.Xaml.Controls;
using System;

namespace AIM.Services;

public class NavigationService : INavigationService
{
    // --- THIS IS THE FIX ---
    // A private field to hold the main navigation frame.
    private Frame _frame;

    // --- THIS IS THE FIX ---
    // The implementation of the Initialize method.
    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void NavigateTo(Type pageType)
    {
        _frame?.Navigate(pageType);
    }

    public void NavigateTo(Type pageType, object parameter)
    {
        _frame?.Navigate(pageType, parameter);
    }
}