using AIM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIM.Services;

public class SearchService : ISearchService
{
    private static readonly HashSet<string> RelevantExtensions = new() { ".txt", ".csv" };

    public async Task<IEnumerable<FileItem>> SearchFilesAsync(string query, string rootPath)
    {
        var results = new List<FileItem>();
        await Task.Run(() => SearchFilesRecursive(rootPath, query.ToLowerInvariant(), results));
        return results;
    }

    public async Task<IEnumerable<FileItem>> SearchContentAsync(string query, string rootPath)
    {
        var results = new List<FileItem>();
        await Task.Run(() => SearchContentRecursive(rootPath, query.ToLowerInvariant(), results));
        return results;
    }

    public async Task<IEnumerable<SearchResultItem>> SearchAsync(SearchOptions options, IProgress<SearchProgress> progress = null)
    {
        var results = new List<SearchResultItem>();
        var progressData = new SearchProgress();

        await Task.Run(() =>
        {
            SearchRecursive(options.RootPath, options, results, progress, progressData);
        });

        return results;
    }

    private void SearchRecursive(string directory, SearchOptions options, List<SearchResultItem> results,
        IProgress<SearchProgress> progress, SearchProgress progressData)
    {
        if (options.CancellationToken.IsCancellationRequested) return;

        try
        {
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                if (options.CancellationToken.IsCancellationRequested) return;

                var ext = Path.GetExtension(file).ToLowerInvariant();

                // Check file type filter
                if (!RelevantExtensions.Contains(ext)) continue;
                if (options.FileTypeFilter == FileTypeFilter.TextOnly && ext != ".txt") continue;
                if (options.FileTypeFilter == FileTypeFilter.CsvOnly && ext != ".csv") continue;

                var fileInfo = new FileInfo(file);

                // Check date filter
                if (options.DateFilter.HasValue && fileInfo.LastWriteTime < options.DateFilter.Value)
                    continue;

                progressData.FilesSearched++;
                progressData.CurrentFile = file;

                var comparison = options.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                var fileName = Path.GetFileName(file);
                bool isMatch = false;
                string matchPreview = string.Empty;

                // Search by file name
                if (options.SearchType == SearchType.FileName || options.SearchType == SearchType.Both)
                {
                    if (fileName.Contains(options.Query, comparison))
                    {
                        isMatch = true;
                    }
                }

                // Search by content
                if (options.SearchType == SearchType.Content || options.SearchType == SearchType.Both)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var index = content.IndexOf(options.Query, comparison);
                        if (index >= 0)
                        {
                            isMatch = true;
                            matchPreview = ExtractMatchPreview(content, index, options.Query.Length);
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read
                    }
                }

                if (isMatch)
                {
                    progressData.MatchesFound++;
                    results.Add(new SearchResultItem
                    {
                        Name = fileName,
                        FullPath = file,
                        DirectoryPath = Path.GetDirectoryName(file) ?? string.Empty,
                        FileType = ext == ".csv" ? FileType.Csv : FileType.Text,
                        FileSize = fileInfo.Length,
                        ModifiedDate = fileInfo.LastWriteTime,
                        MatchPreview = matchPreview
                    });
                }

                // Report progress every 10 files
                if (progressData.FilesSearched % 10 == 0)
                {
                    progress?.Report(new SearchProgress
                    {
                        FilesSearched = progressData.FilesSearched,
                        MatchesFound = progressData.MatchesFound,
                        CurrentFile = file
                    });
                }
            }

            // Recurse into subdirectories
            foreach (var subDir in Directory.GetDirectories(directory))
            {
                if (options.CancellationToken.IsCancellationRequested) return;
                SearchRecursive(subDir, options, results, progress, progressData);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
        catch (DirectoryNotFoundException)
        {
            // Skip directories that no longer exist
        }
    }

    private string ExtractMatchPreview(string content, int matchIndex, int matchLength)
    {
        const int contextLength = 50;

        var start = Math.Max(0, matchIndex - contextLength);
        var end = Math.Min(content.Length, matchIndex + matchLength + contextLength);

        var preview = content.Substring(start, end - start);

        // Clean up whitespace
        preview = Regex.Replace(preview, @"\s+", " ").Trim();

        // Add ellipsis if truncated
        if (start > 0) preview = "..." + preview;
        if (end < content.Length) preview = preview + "...";

        return preview;
    }

    private void SearchFilesRecursive(string directory, string query, List<FileItem> results)
    {
        try
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!RelevantExtensions.Contains(ext)) continue;

                if (Path.GetFileName(file).ToLowerInvariant().Contains(query))
                {
                    results.Add(new FileItem
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        Type = ext == ".csv" ? FileType.Csv : FileType.Text
                    });
                }
            }

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                SearchFilesRecursive(subDir, query, results);
            }
        }
        catch { }
    }

    private void SearchContentRecursive(string directory, string query, List<FileItem> results)
    {
        try
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!RelevantExtensions.Contains(ext)) continue;

                try
                {
                    var content = File.ReadAllText(file);
                    if (content.ToLowerInvariant().Contains(query))
                    {
                        results.Add(new FileItem
                        {
                            Name = Path.GetFileName(file),
                            FullPath = file,
                            Type = ext == ".csv" ? FileType.Csv : FileType.Text
                        });
                    }
                }
                catch { }
            }

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                SearchContentRecursive(subDir, query, results);
            }
        }
        catch { }
    }
}