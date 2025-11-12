using AIM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

public interface IFileService
{
    IEnumerable<FileItem> GetFiles(string directoryPath);
    void PopulateSubDirectories(DirectoryItem parent);
    Task<string> ReadFilePreviewAsync(string filePath);
    Task WriteFileAsync(string filePath, string content);
}