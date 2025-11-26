using System;

namespace AIM.Services;

public delegate void NavigationChangedHandler(Type pageType);

public interface INavigationService
{
    bool CanGoBack { get; }
    void Initialize(Microsoft.UI.Xaml.Controls.Frame frame);
    void NavigateTo(Type pageType);
    void NavigateTo(Type pageType, object parameter);
    void GoBack();
    event NavigationChangedHandler NavigationChanged;
}