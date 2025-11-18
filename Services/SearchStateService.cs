using AIM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace AIM.Services;

/// <summary>
/// Service for preserving search state across navigation using local app data.
/// </summary>
public class SearchStateService : ISearchStateService
{
    private const string StateFileName = "search_state.json";
    private readonly string _stateFilePath;

    public SearchStateService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var aimDataPath = Path.Combine(appDataPath, "AIM");
        Directory.CreateDirectory(aimDataPath);
        _stateFilePath = Path.Combine(aimDataPath, StateFileName);
    }

    public void SaveSearchState(string searchQuery, string searchDirectory, bool isContentSearch, IEnumerable<FileItem> searchResults)
    {
        try
        {
            var state = new SearchState
            {
                SearchQuery = searchQuery,
                SearchDirectory = searchDirectory,
                IsContentSearch = isContentSearch,
                SearchResults = searchResults.ToList()  // NEW: Save results
            };

            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(_stateFilePath, json);
            System.Diagnostics.Debug.WriteLine($"[SearchStateService] Search state saved: {searchQuery} ({state.SearchResults.Count} results)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SearchStateService] Error saving state: {ex.Message}");
        }
    }

    public SearchState? LoadSearchState()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
                return null;

            var json = File.ReadAllText(_stateFilePath);
            var state = JsonSerializer.Deserialize<SearchState>(json);
            System.Diagnostics.Debug.WriteLine($"[SearchStateService] Search state loaded: {state?.SearchQuery} ({state?.SearchResults.Count} results)");
            return state;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SearchStateService] Error loading state: {ex.Message}");
            return null;
        }
    }

    public void ClearSearchState()
    {
        try
        {
            if (File.Exists(_stateFilePath))
                File.Delete(_stateFilePath);

            System.Diagnostics.Debug.WriteLine("[SearchStateService] Search state cleared");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SearchStateService] Error clearing state: {ex.Message}");
        }
    }
}