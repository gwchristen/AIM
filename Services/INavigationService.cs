using System;

namespace AIM.Services;

public interface INavigationService
{
    void SetFrame(Microsoft.UI.Xaml.Controls.Frame frame);
    void NavigateTo(Type pageType, object parameter = null);
}