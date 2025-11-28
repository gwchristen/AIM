using System;

namespace AIM.Services;

/// <summary>
/// Service for broadcasting refresh requests across the application. 
/// </summary>
public interface IRefreshService
{
    /// <summary>
    /// Event fired when a global refresh is requested. 
    /// </summary>
    event EventHandler RefreshRequested;

    /// <summary>
    /// Requests all subscribed components to refresh their data.
    /// </summary>
    void RequestRefresh();
}

public class RefreshService : IRefreshService
{
    public event EventHandler RefreshRequested;

    public void RequestRefresh()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }
}