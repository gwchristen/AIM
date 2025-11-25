using AIM.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class FileService : IFileService
{
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
        catch (IOException)
        {
            return Enumerable.Empty<FileItem>();
        }
    }

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
        catch { /* Ignore errors */ }
    }

    public async Task<string> ReadFilePreviewAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task WriteFileAsync(string filePath, string content)
    {
        await File.WriteAllTextAsync(filePath, content);
    }
}