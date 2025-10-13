using System.IO.Compression;
using System.Threading.Tasks;

namespace AIM.Services;

public class BackupService : IBackupService
{
    public async Task CreateZipBackupAsync(string sourcePath, string zipPath)
    {
        await Task.Run(() => ZipFile.CreateFromDirectory(sourcePath, zipPath));
    }
}