using System.Threading.Tasks;

namespace AIM.Services;

public interface IBackupService
{
    Task CreateZipBackupAsync(string sourcePath, string zipPath);
}