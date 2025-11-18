using System;

namespace AIM.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    void Initialize(Microsoft.UI.Xaml.Controls.Frame frame);
    void NavigateTo(Type pageType);
    void NavigateTo(Type pageType, object parameter);
    void NavigateTo(Type pageType, string navigationTag);
    void NavigateTo(Type pageType, object parameter, string navigationTag);
    void GoBack();
    event Action<string>? NavigationRequested;
}