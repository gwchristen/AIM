using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public class BrowseViewModel : ObservableObject
{
    private readonly IFileService _fileService;

    public ObservableCollection<FileItem> Files { get; } = new();

    public BrowseViewModel()
    {
        _fileService = Ioc.Default.GetService<IFileService>();
    }

    public async Task LoadFilesAsync(string path)
    {
        Files.Clear();
        try
        {
            // Load files (expand to include subdirs if needed)
            var files = Directory.GetFiles(path)
                .Select(f => new FileItem
                {
                    Name = Path.GetFileName(f),
                    FullPath = f,
                    Size = new FileInfo(f).Length,
                    LastModified = File.GetLastWriteTime(f),
                    Type = GetFileType(f)
                });
            foreach (var file in files)
            {
                Files.Add(file);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Handle access issues (optional: show message)
        }
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