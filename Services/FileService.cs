using AIM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

/// <summary>
/// Provides file and directory operations for the application.
/// Implements <see cref="IFileService"/> with robust error handling for common file system exceptions.
/// </summary>
public class FileService : IFileService
{
    /// <summary>
    /// Gets all files in the specified directory.
    /// Errors may occur due to insufficient permissions or I/O issues.
    /// Returns an empty collection on error to allow the application to continue gracefully.
    /// </summary>
    /// <param name="directoryPath">The path to the directory to search.</param>
    /// <returns>A collection of <see cref="FileItem"/> objects representing files in the directory, or an empty collection if an error occurs.</returns>
    public IEnumerable<FileItem> GetFiles(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            return Enumerable.Empty<FileItem>();
        }

        try
        {
            return Directory.GetFiles(directoryPath).Select(f => new FileItem { Name = Path.GetFileName(f), FullPath = f });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Access to the directory was denied - this can occur when accessing system or protected directories
            Debug.WriteLine($"[FileService] Access denied to directory: {directoryPath}. Error: {ex.Message}");
            return Enumerable.Empty<FileItem>();
        }
        catch (IOException ex)
        {
            // I/O error occurred - this can happen with network drives, locked files, or disk issues
            Debug.WriteLine($"[FileService] I/O error reading directory: {directoryPath}. Error: {ex.Message}");
            return Enumerable.Empty<FileItem>();
        }
    }

    /// <summary>
    /// Recursively populates subdirectories for a directory tree.
    /// Errors may occur due to insufficient permissions, I/O issues, or path length limitations.
    /// Errors are logged but don't stop the entire tree population - this allows partial
    /// directory trees to be shown even when some subdirectories are inaccessible.
    /// </summary>
    /// <param name="parent">The parent <see cref="DirectoryItem"/> to populate with subdirectories.</param>
    public void PopulateSubDirectories(DirectoryItem parent)
    {
        try
        {
            var subs = Directory.GetDirectories(parent.FullPath);
            foreach (var sub in subs)
            {
                var child = new DirectoryItem { Name = Path.GetFileName(sub), FullPath = sub };
                PopulateSubDirectories(child);
                parent.SubDirectories.Add(child);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // Access to the directory was denied - this commonly occurs with system directories,
            // hidden folders, or directories with restricted permissions
            Debug.WriteLine($"[FileService] Access denied to subdirectories of: {parent.FullPath}. Error: {ex.Message}");
        }
        catch (IOException ex)
        {
            // I/O error occurred - this can happen with network drives, symbolic links, or disk issues
            Debug.WriteLine($"[FileService] I/O error accessing subdirectories of: {parent.FullPath}. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors (e.g., PathTooLongException, NotSupportedException)
            Debug.WriteLine($"[FileService] Unexpected error accessing subdirectories of: {parent.FullPath}. Error: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously reads the entire contents of a text file.
    /// </summary>
    /// <param name="filePath">The full path to the file to read.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the file contents as a string.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to read the file.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    public async Task<string> ReadFilePreviewAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }

    /// <summary>
    /// Asynchronously writes text content to a file, creating it if it doesn't exist or overwriting it if it does.
    /// </summary>
    /// <param name="filePath">The full path to the file to write.</param>
    /// <param name="content">The content to write to the file.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to write to the file.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    public async Task WriteFileAsync(string filePath, string content)
    {
        await File.WriteAllTextAsync(filePath, content);
    }
}