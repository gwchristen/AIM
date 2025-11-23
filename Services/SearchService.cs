using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class SearchService : ISearchService
{
    private readonly IFileService _fileService;

    public SearchService()
    {
        _fileService = Ioc.Default.GetService<IFileService>();
    }

    public async Task<IEnumerable<FileItem>> SearchFilesAsync(string query, string rootPath)
    {
        var results = new List<FileItem>();
        if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
        {
            SearchDirectory(rootPath, query, results);
        }
        return results;
    }

    public async Task<IEnumerable<FileItem>> SearchContentAsync(string query, string rootPath)
    {
        var results = new List<FileItem>();
        if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
        {
            await SearchDirectoryContent(rootPath, query, results);
        }
        return results;
    }

    private void SearchDirectory(string path, string query, List<FileItem> results)
    {
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
        catch { }
    }

    private async Task SearchDirectoryContent(string path, string query, List<FileItem> results)
    {
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
                    catch { }
                }
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                await SearchDirectoryContent(dir, query, results);
                if (results.Count >= 100) return;
            }
        }
        catch { }
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