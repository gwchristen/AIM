using AIM.Models;
using System.Collections.Generic;

namespace AIM.Services;

/// <summary>
/// Service for preserving search state across navigation.
/// </summary>
public interface ISearchStateService
{
    /// <summary>
    /// Saves the current search state including results.
    /// </summary>
    void SaveSearchState(string searchQuery, string searchDirectory, bool isContentSearch, IEnumerable<FileItem> searchResults);

    /// <summary>
    /// Loads the saved search state.
    /// </summary>
    SearchState? LoadSearchState();

    /// <summary>
    /// Clears the saved search state.
    /// </summary>
    void ClearSearchState();
}

/// <summary>
/// Represents the state of a search operation.
/// </summary>
public class SearchState
{
    public string SearchQuery { get; set; } = string.Empty;
    public string SearchDirectory { get; set; } = string.Empty;
    public bool IsContentSearch { get; set; } = true;
    public List<FileItem> SearchResults { get; set; } = new();  // NEW
}