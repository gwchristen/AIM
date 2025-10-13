using AIM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class FileService : IFileService
{
    public async Task LoadDirectoryAsync(DirectoryItem item)
    {
        if (item == null) return;
        try
        {
            var subDirs = Directory.GetDirectories(item.FullPath)
                .Select(d => new DirectoryItem { Name = Path.GetFileName(d), FullPath = d })
                .ToList();
            item.SubDirectories.Clear();
            foreach (var sub in subDirs)
            {
                item.SubDirectories.Add(sub);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Handle access denied (optional: log or skip)
        }
    }

    public async Task<string> ReadFilePreviewAsync(string path, long maxSize = 5242880)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        var buffer = new char[maxSize];
        int charsRead = await reader.ReadAsync(buffer, 0, (int)maxSize);
        return new string(buffer, 0, charsRead);
    }

    public async Task WriteFileAsync(string path, string content)
    {
        await File.WriteAllTextAsync(path, content);
    }

    public async Task MoveFilesAsync(IEnumerable<FileItem> files, string destination)
    {
        foreach (var file in files)
        {
            string destPath = Path.Combine(destination, Path.GetFileName(file.FullPath));
            File.Move(file.FullPath, destPath);
        }
    }

    public async Task RenameAsync(string path, string newName)
    {
        string newPath = Path.Combine(Path.GetDirectoryName(path)!, newName);
        File.Move(path, newPath);
    }

    public async Task CreateFileAsync(string path)
    {
        File.Create(path).Close();
    }

    public async Task CreateFolderAsync(string path)
    {
        Directory.CreateDirectory(path);
    }

    public async Task IndexFilesAsync(string rootPath, IEnumerable<string> extensions)
    {
        // Placeholder for indexing
    }
}