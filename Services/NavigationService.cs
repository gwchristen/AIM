using Microsoft.UI.Xaml.Controls;
using System;

namespace AIM.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly IAuditLoggingService _auditLoggingService;

    public NavigationService(IAuditLoggingService auditLoggingService)
    {
        _auditLoggingService = auditLoggingService;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public event Action<string>? NavigationRequested;

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
        
        _auditLoggingService.LogAudit(
            "PAGE_NAVIGATION",
            null,
            $"Navigated to {pageType.Name}"
        );
    }

    public void NavigateTo(Type pageType, object parameter)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Navigation frame has not been initialized.");
        }
        _frame.Navigate(pageType, parameter);
        
        _auditLoggingService.LogAudit(
            "PAGE_NAVIGATION",
            null,
            $"Navigated to {pageType.Name} with parameter"
        );
    }

    public void NavigateTo(Type pageType, string navigationTag)
    {
        NavigationRequested?.Invoke(navigationTag);
        NavigateTo(pageType);
    }

    public void NavigateTo(Type pageType, object parameter, string navigationTag)
    {
        NavigationRequested?.Invoke(navigationTag);
        NavigateTo(pageType, parameter);
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            _frame?.GoBack();
            
            _auditLoggingService.LogAudit(
                "PAGE_NAVIGATION_BACK",
                null,
                "Navigated back"
            );
        }
    }
}