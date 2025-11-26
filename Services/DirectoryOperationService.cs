using AIM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public class DirectoryOperationService
{
    private readonly IPrintPaginationService _paginationService;

    public DirectoryOperationService(IPrintPaginationService paginationService)
    {
        _paginationService = paginationService;
    }

    // Parameterless constructor for backward compatibility
    public DirectoryOperationService() : this(new PrintPaginationService())
    {
    }

    public async Task<PrintableForm> GenerateFormDataAsync(string opCoDirectoryPath)
    {
        var form = new PrintableForm
        {
            Header = "Form Data Generation",
            SubHeader = "Inventory Summary"
        };

        // Collect all items first (flat list)
        var allItems = new List<PrintableFormItem>();

        await Task.Run(() =>
        {
            try
            {
                var opCoDirInfo = new DirectoryInfo(opCoDirectoryPath);
                form.Header = opCoDirInfo.Name;

                var level2Dirs = opCoDirInfo.GetDirectories().OrderBy(d => d.Name);

                if (!level2Dirs.Any())
                {
                    allItems.Add(new PrintableFormItem { Content = "Error: No 'Level 2' subdirectories found in the selected folder.", Type = RowType.Level3Header_C });
                    return;
                }

                foreach (var level2Dir in level2Dirs)
                {
                    allItems.Add(new PrintableFormItem { Content = level2Dir.Name, Type = RowType.Level2Header });

                    var filesInLevel2 = level2Dir.GetFiles().OrderBy(f => f.Name);
                    foreach (var file in filesInLevel2)
                    {
                        allItems.Add(new PrintableFormItem { Content = Path.GetFileNameWithoutExtension(file.Name), Type = RowType.File });
                    }

                    var level3Dirs = level2Dir.GetDirectories().OrderBy(d => d.Name);
                    foreach (var level3Dir in level3Dirs)
                    {
                        string dirName = level3Dir.Name;
                        RowType headerType;

                        if (dirName.Contains("3a", StringComparison.OrdinalIgnoreCase)) headerType = RowType.Level3Header_A;
                        else if (dirName.Contains("3b", StringComparison.OrdinalIgnoreCase)) headerType = RowType.Level3Header_B;
                        else if (dirName.Contains("3c", StringComparison.OrdinalIgnoreCase)) headerType = RowType.Level3Header_C;
                        else headerType = RowType.Level3Header_A;

                        allItems.Add(new PrintableFormItem { Content = dirName, Type = headerType });

                        if (headerType == RowType.Level3Header_C)
                        {
                            foreach (var file in level3Dir.GetFiles().OrderBy(f => f.Name))
                            {
                                try
                                {
                                    var lines = File.ReadLines(file.FullName).Where(line => !string.IsNullOrWhiteSpace(line));
                                    foreach (var line in lines)
                                    {
                                        allItems.Add(new PrintableFormItem { Content = line, Type = RowType.File });
                                    }
                                }
                                catch (IOException ex)
                                {
                                    allItems.Add(new PrintableFormItem { Content = $"IO Error reading {file.Name}: {ex.Message}", Type = RowType.File });
                                }
                            }
                        }
                        else
                        {
                            foreach (var file in level3Dir.GetFiles().OrderBy(f => f.Name))
                            {
                                allItems.Add(new PrintableFormItem { Content = Path.GetFileNameWithoutExtension(file.Name), Type = RowType.File });
                            }
                        }
                    }

                    // Add a blank row after each Level 2 section
                    allItems.Add(new PrintableFormItem { Type = RowType.Blank });
                }
            }
            catch (Exception ex)
            {
                allItems.Clear();
                form.Header = "An Error Occurred";
                allItems.Add(new PrintableFormItem { Content = "A critical error stopped form generation.", Type = RowType.Level2Header });
                allItems.Add(new PrintableFormItem { Content = ex.Message, Type = RowType.File });
                allItems.Add(new PrintableFormItem { Type = RowType.Blank });
                allItems.Add(new PrintableFormItem { Content = ex.StackTrace ?? "No stack trace available", Type = RowType.File });
            }
        });

        // Now paginate the content intelligently
        form.Pages = _paginationService.PaginateContent(form.Header, allItems);

        return form;
    }

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
            foreach (var opCoDir in rootDir.GetDirectories())
            {
                var counter = 1;
                var allFiles = opCoDir.GetFiles("*", SearchOption.AllDirectories).OrderBy(f => f.FullName).ToList();
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
            foreach (var opCoDir in rootDir.GetDirectories())
            {
                var files = Directory.GetFiles(opCoDir.FullName, "*.*", SearchOption.AllDirectories);
                long deviceCount = 0;
                foreach (var file in files)
                {
                    try
                    {
                        deviceCount += File.ReadLines(file).Count(line => !string.IsNullOrWhiteSpace(line));
                    }
                    catch (IOException) { }
                }
                stats.Add(new OpCoStatItem { OpCoName = opCoDir.Name, FileCount = files.Length, DeviceCount = deviceCount });
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
            if (!rootDir.Exists)
            {
                throw new DirectoryNotFoundException("Root directory not found.");
            }
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
