using AIM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class SearchService : ISearchService
{
    public async Task<IEnumerable<FileItem>> SearchFilesAsync(string query, string rootPath)
    {
        var files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileItem
            {
                Name = Path.GetFileName(f),
                FullPath = f,
                Type = GetFileType(f)
            });
        return files;
    }

    public async Task<IEnumerable<FileItem>> SearchContentAsync(string query, string rootPath)
    {
        return Enumerable.Empty<FileItem>(); // Implement later
    }

    private FileType GetFileType(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".txt" => FileType.Text,
            ".csv" => FileType.Csv,
            ".log" => FileType.Log,
            _ => FileType.Other
        };
    }
}