using Microsoft.UI.Xaml.Controls;
using System;

namespace AIM.Services;

public interface INavigationService
{
    void Initialize(Frame frame);

    void NavigateTo(Type pageType);
    void NavigateTo(Type pageType, object parameter);
}