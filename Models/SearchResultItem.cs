using System;
using System.Threading;

namespace AIM.Models;

public class SearchResultItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string MatchPreview { get; set; } = string.Empty;
    public bool HasMatchPreview => !string.IsNullOrEmpty(MatchPreview);

    public string FileIcon => FileType switch
    {
        FileType.Csv => "\uE9D9",
        FileType.Text => "\uE8A5",
        _ => "\uE8A5"
    };

    public string FileSizeText
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = FileSize;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0. ##} {sizes[order]}";
        }
    }

    public string ModifiedDateText => ModifiedDate.ToString("g");
}

public enum SearchType
{
    Content = 0,
    FileName = 1,
    Both = 2
}

public enum FileTypeFilter
{
    All = 0,
    TextOnly = 1,
    CsvOnly = 2
}

public class SearchOptions
{
    public string Query { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public SearchType SearchType { get; set; } = SearchType.Content;
    public FileTypeFilter FileTypeFilter { get; set; } = FileTypeFilter.All;
    public bool IsCaseSensitive { get; set; } = false;
    public bool UseWildcards { get; set; } = false;  // NEW
    public DateTime? DateFilter { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public class SearchProgress
{
    public int FilesSearched { get; set; }
    public int MatchesFound { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
}