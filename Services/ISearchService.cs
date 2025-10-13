using AIM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIM.Services;

public interface ISearchService
{
    Task<IEnumerable<FileItem>> SearchFilesAsync(string query, string rootPath);
    Task<IEnumerable<FileItem>> SearchContentAsync(string query, string rootPath);
}