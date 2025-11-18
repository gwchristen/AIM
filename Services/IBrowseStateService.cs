namespace AIM.Services;

/// <summary>
/// Service for preserving browse state across navigation.
/// </summary>
public interface IBrowseStateService
{
    /// <summary>
    /// Saves the current browse state (directories and breadcrumbs).
    /// </summary>
    void SaveBrowseState(string leftDirectory, string rightDirectory);

    /// <summary>
    /// Loads the saved browse state.
    /// </summary>
    BrowseState? LoadBrowseState();

    /// <summary>
    /// Clears the saved browse state.
    /// </summary>
    void ClearBrowseState();
}

/// <summary>
/// Represents the state of a browse session.
/// </summary>
public class BrowseState
{
    public string LeftDirectory { get; set; } = string.Empty;
    public string RightDirectory { get; set; } = string.Empty;
}