using AIM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class DirectoryOperationService
{
    public Task CopyDirectoryStructureAsync(string sourceDir, string destinationDir, string newDirName)
    {
        return Task.Run(() =>
        {
            var newDestination = Path.Combine(destinationDir, newDirName);
            if (Directory.Exists(newDestination))
            {
                throw new IOException($"A directory named '{newDirName}' already exists in the destination.");
            }

            Directory.CreateDirectory(newDestination);

            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, newDestination));
            }
        });
    }

    public Task<PrintableForm> GenerateFormDataAsync(string opCoDirectoryPath)
    {
        return Task.Run(() =>
        {
            var opCoDirInfo = new DirectoryInfo(opCoDirectoryPath);
            var form = new PrintableForm
            {
                OpCoHeader = opCoDirInfo.Name
            };

            var level2Dirs = opCoDirInfo.GetDirectories().OrderBy(d => d.Name);

            foreach (var level2Dir in level2Dirs)
            {
                var currentPage = new FormPage { PageHeader = level2Dir.Name };

                var filesInLevel2 = level2Dir.GetFiles().OrderBy(f => f.Name);
                if (filesInLevel2.Any())
                {
                    foreach (var file in filesInLevel2)
                    {
                        currentPage.Rows.Add(new FormRow { Content = Path.GetFileNameWithoutExtension(file.Name), Type = RowType.File });
                    }
                    currentPage.Rows.Add(new FormRow { Type = RowType.Blank });
                    currentPage.Rows.Add(new FormRow { Type = RowType.Blank });
                }

                var level3Dirs = level2Dir.GetDirectories().OrderBy(d => d.Name);
                foreach (var level3Dir in level3Dirs)
                {
                    currentPage.Rows.Add(new FormRow { Content = level3Dir.Name, Type = RowType.Level3Header });
                    foreach (var file in level3Dir.GetFiles().OrderBy(f => f.Name))
                    {
                        currentPage.Rows.Add(new FormRow { Content = Path.GetFileNameWithoutExtension(file.Name), Type = RowType.File });
                    }
                    currentPage.Rows.Add(new FormRow { Type = RowType.Blank });
                    currentPage.Rows.Add(new FormRow { Type = RowType.Blank });
                }

                form.Pages.Add(currentPage);
            }

            return form;
        });
    }

    public Task<Dictionary<string, int>> RenameFilesSequentiallyAsync(string rootDirectoryPath)
    {
        return Task.Run(() =>
        {
            var rootDir = new DirectoryInfo(rootDirectoryPath);
            if (!rootDir.Exists)
            {
                throw new DirectoryNotFoundException("The selected root directory does not exist.");
            }

            var renamedFilesCount = new Dictionary<string, int>();
            var opCoDirs = rootDir.GetDirectories();

            foreach (var opCoDir in opCoDirs)
            {
                var counter = 1;
                var allFiles = opCoDir.GetFiles("*", SearchOption.AllDirectories)
                                      .OrderBy(f => f.FullName)
                                      .ToList();

                foreach (var file in allFiles)
                {
                    var newFileName = $"{opCoDir.Name}_{counter:D4}{file.Extension}";
                    var newFilePath = Path.Combine(file.DirectoryName!, newFileName);

                    if (File.Exists(newFilePath) && !string.Equals(file.FullName, newFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        newFileName = $"{opCoDir.Name}_{counter:D4}_{Guid.NewGuid().ToString().Substring(0, 4)}{file.Extension}";
                        newFilePath = Path.Combine(file.DirectoryName!, newFileName);
                    }

                    file.MoveTo(newFilePath);
                    counter++;
                }
                renamedFilesCount[opCoDir.Name] = counter - 1;
            }
            return renamedFilesCount;
        });
    }

    public Task<List<OpCoStatItem>> GetDirectoryStatsAsync(string rootDirectoryPath)
    {
        return Task.Run(() =>
        {
            var rootDir = new DirectoryInfo(rootDirectoryPath);
            if (!rootDir.Exists)
            {
                throw new DirectoryNotFoundException("The selected root directory does not exist.");
            }

            var stats = new List<OpCoStatItem>();
            var opCoDirs = rootDir.GetDirectories();

            foreach (var opCoDir in opCoDirs)
            {
                var files = Directory.GetFiles(opCoDir.FullName, "*.*", SearchOption.AllDirectories);
                long deviceCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        deviceCount += File.ReadLines(file).Count(line => !string.IsNullOrWhiteSpace(line));
                    }
                    catch (IOException)
                    {
                    }
                }

                stats.Add(new OpCoStatItem
                {
                    OpCoName = opCoDir.Name,
                    FileCount = files.Length,
                    DeviceCount = deviceCount
                });
            }

            return stats;
        });
    }

    public Task<FileAnomalyReport> FindFileAnomaliesAsync(string rootDirectoryPath)
    {
        return Task.Run(() =>
        {
            var report = new FileAnomalyReport();
            var rootDir = new DirectoryInfo(rootDirectoryPath);
            if (!rootDir.Exists) throw new DirectoryNotFoundException("Root directory not found.");

            string ohioPath = Path.Combine(rootDirectoryPath, "Ohio");
            string imPath = Path.Combine(rootDirectoryPath, "I&M");

            string[] allFiles = Directory.GetFiles(rootDirectoryPath, "*", SearchOption.AllDirectories);
            var imTerms = new[] { "I&M", "I+M", "IM" };

            foreach (string file in allFiles)
            {
                string fileName = Path.GetFileName(file);
                bool isOhFile = fileName.Contains("OH", StringComparison.OrdinalIgnoreCase);
                bool isImFile = imTerms.Any(term => fileName.Contains(term, StringComparison.OrdinalIgnoreCase));

                if (isOhFile && !file.StartsWith(ohioPath, StringComparison.OrdinalIgnoreCase))
                {
                    report.MisplacedOhFiles.Add(file);
                }

                if (isImFile && !file.StartsWith(imPath, StringComparison.OrdinalIgnoreCase))
                {
                    report.MisplacedImFiles.Add(file);
                }

                if (!isOhFile && !isImFile)
                {
                    report.UnidentifiedFiles.Add(file);
                }
            }
            return report;
        });
    }
}