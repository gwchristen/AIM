using AIM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

public interface IFileService
{
    Task LoadDirectoryAsync(DirectoryItem item);
    Task<string> ReadFilePreviewAsync(string path, long maxSize = 5242880);
    Task WriteFileAsync(string path, string content);
    Task MoveFilesAsync(IEnumerable<FileItem> files, string destination);
    Task RenameAsync(string path, string newName);
    Task CreateFileAsync(string path);
    Task CreateFolderAsync(string path);
    Task IndexFilesAsync(string rootPath, IEnumerable<string> extensions);
}