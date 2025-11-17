using AIM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class SearchService : ISearchService
{
    private readonly IFileService _fileService;

    // Use constructor injection instead of the service locator.
    public SearchService(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// Searches for files by name in the directory tree.
    /// Limited to 100 results for performance - prevents UI freezing and excessive memory usage
    /// when searching large directory structures.
    /// </summary>
    public async Task<IEnumerable<FileItem>> SearchFilesAsync(string query, string rootPath)
    {
        Debug.WriteLine($"[SearchService] Starting file name search for query: '{query}' in path: {rootPath}");
        var results = new List<FileItem>();
        if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
        {
            SearchDirectory(rootPath, query, results);
        }
        Debug.WriteLine($"[SearchService] File name search completed. Found {results.Count} results.");
        return results;
    }

    /// <summary>
    /// Searches for files by content in the directory tree.
    /// Limited to 100 results for performance - prevents UI freezing and excessive memory usage.
    /// Only searches text-based file types (txt, csv, log) to avoid reading binary files.
    /// </summary>
    public async Task<IEnumerable<FileItem>> SearchContentAsync(string query, string rootPath)
    {
        Debug.WriteLine($"[SearchService] Starting content search for query: '{query}' in path: {rootPath}");
        var results = new List<FileItem>();
        if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
        {
            await SearchDirectoryContent(rootPath, query, results);
        }
        Debug.WriteLine($"[SearchService] Content search completed. Found {results.Count} results.");
        return results;
    }

    /// <summary>
    /// Recursively searches directories for files matching the query by name.
    /// Results are limited to 100 items to maintain UI responsiveness - searching deep
    /// directory structures with thousands of files can freeze the UI if unlimited.
    /// </summary>
    private void SearchDirectory(string path, string query, List<FileItem> results)
    {
        // Performance optimization: stop searching once we have 100 results
        if (results.Count >= 100) return;
        try
        {
            var files = Directory.GetFiles(path).Where(f => Path.GetFileName(f).Contains(query, StringComparison.OrdinalIgnoreCase));
            foreach (var f in files)
            {
                results.Add(new FileItem
                {
                    Name = Path.GetFileName(f),
                    FullPath = f,
                    Type = GetFileType(f)
                });
                if (results.Count >= 100) return;
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                SearchDirectory(dir, query, results);
                if (results.Count >= 100) return;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // Access to the directory was denied - this commonly occurs with system directories
            // or protected folders. Log and continue searching other accessible directories.
            Debug.WriteLine($"[SearchService] Access denied while searching: {path}. Error: {ex.Message}");
        }
        catch (IOException ex)
        {
            // I/O error occurred - this can happen with network drives or disk issues
            Debug.WriteLine($"[SearchService] I/O error while searching: {path}. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors (e.g., PathTooLongException)
            Debug.WriteLine($"[SearchService] Unexpected error while searching: {path}. Error: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Recursively searches directories for files containing the query in their content.
    /// Results are limited to 100 items to prevent excessive memory usage and maintain
    /// UI responsiveness when searching large directory trees with many text files.
    /// Only searches text-based file types to avoid reading large binary files.
    /// </summary>
    private async Task SearchDirectoryContent(string path, string query, List<FileItem> results)
    {
        // Performance optimization: stop searching once we have 100 results
        if (results.Count >= 100) return;
        try
        {
            var files = Directory.GetFiles(path);
            foreach (var f in files)
            {
                var type = GetFileType(f);
                if (type == FileType.Text || type == FileType.Csv || type == FileType.Log)
                {
                    try
                    {
                        // This call will now succeed because IFileService has the method.
                        var content = await _fileService.ReadFilePreviewAsync(f);
                        if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new FileItem
                            {
                                Name = Path.GetFileName(f),
                                FullPath = f,
                                Type = type
                            });
                            if (results.Count >= 100) return;
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // File is locked or requires elevated permissions
                        Debug.WriteLine($"[SearchService] Access denied reading file: {f}. Error: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        // File is in use, locked, or I/O error occurred
                        Debug.WriteLine($"[SearchService] I/O error reading file: {f}. Error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // Catch other exceptions like OutOfMemoryException for very large files
                        Debug.WriteLine($"[SearchService] Error reading file: {f}. Error: {ex.GetType().Name} - {ex.Message}");
                    }
                }
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                await SearchDirectoryContent(dir, query, results);
                if (results.Count >= 100) return;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // Access to the directory was denied
            Debug.WriteLine($"[SearchService] Access denied while searching directory content: {path}. Error: {ex.Message}");
        }
        catch (IOException ex)
        {
            // I/O error occurred accessing the directory
            Debug.WriteLine($"[SearchService] I/O error while searching directory content: {path}. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors
            Debug.WriteLine($"[SearchService] Unexpected error while searching directory content: {path}. Error: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private FileType GetFileType(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".txt" => FileType.Text,
            ".csv" => FileType.Csv,
            ".log" => FileType.Log,
            _ => FileType.Other
        };
    }
}