using System;
using System.IO;
using System.Text.Json;

namespace AIM.Services;

/// <summary>
/// Service for preserving browse state across navigation using local app data.
/// </summary>
public class BrowseStateService : IBrowseStateService
{
    private const string StateFileName = "browse_state.json";
    private readonly string _stateFilePath;

    public BrowseStateService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var aimDataPath = Path.Combine(appDataPath, "AIM");
        Directory.CreateDirectory(aimDataPath);
        _stateFilePath = Path.Combine(aimDataPath, StateFileName);
        System.Diagnostics.Debug.WriteLine($"[BrowseStateService] Constructor - State file path: {_stateFilePath}");
    }

    public void SaveBrowseState(string leftDirectory, string rightDirectory)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] SaveBrowseState called with Left: {leftDirectory}, Right: {rightDirectory}");

            var state = new BrowseState
            {
                LeftDirectory = leftDirectory,
                RightDirectory = rightDirectory
            };

            var json = JsonSerializer.Serialize(state);
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] Serialized JSON: {json}");

            File.WriteAllText(_stateFilePath, json);
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✓ Browse state saved successfully to {_stateFilePath}");

            // Verify file was written
            if (File.Exists(_stateFilePath))
            {
                var fileSize = new FileInfo(_stateFilePath).Length;
                System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✓ File verified - Size: {fileSize} bytes");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✗ Error saving state: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✗ Stack trace: {ex.StackTrace}");
        }
    }

    public BrowseState? LoadBrowseState()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] LoadBrowseState called");
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] Looking for file at: {_stateFilePath}");

            if (!File.Exists(_stateFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✗ File does not exist");
                return null;
            }

            var fileSize = new FileInfo(_stateFilePath).Length;
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✓ File exists - Size: {fileSize} bytes");

            var json = File.ReadAllText(_stateFilePath);
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✓ File read - JSON content: {json}");

            var state = JsonSerializer.Deserialize<BrowseState>(json);
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✓ Deserialized - Left: {state?.LeftDirectory}, Right: {state?.RightDirectory}");

            return state;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✗ Error loading state: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✗ Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public void ClearBrowseState()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ClearBrowseState called");

            if (File.Exists(_stateFilePath))
            {
                File.Delete(_stateFilePath);
                System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✓ Browse state cleared");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BrowseStateService] File not found to delete");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseStateService] ✗ Error clearing state: {ex.Message}");
        }
    }
}