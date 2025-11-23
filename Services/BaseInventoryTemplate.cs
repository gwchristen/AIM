using AIM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public abstract class BaseInventoryTemplate : IFormTemplate
{
    public abstract string TemplateName { get; }

    protected virtual int AvailableRowsPerPage => 50;

    protected virtual RowType[] Level3HeaderTypes => new[]
    {
        RowType.Level3Header_A,
        RowType.Level3Header_B,
        RowType.Level3Header_C
    };

    /// <summary>
    /// Special directories that get their own page per Level 3 section.
    /// </summary>
    protected virtual string[] SpecialDirectories => new[] { "Workstations" };

    public async Task<PrintableForm> GenerateAsync(string directoryPath)
    {
        var form = new PrintableForm
        {
            Header = TemplateName,
            SubHeader = "Inventory Summary"
        };

        await Task.Run(() =>
        {
            try
            {
                var opCoDirInfo = new DirectoryInfo(directoryPath);
                form.Header = opCoDirInfo.Name;

                var level2Dirs = opCoDirInfo.GetDirectories().OrderBy(d => d.Name).ToList();

                if (!level2Dirs.Any())
                {
                    form.Pages.Add(CreateErrorPage("No subdirectories found in the selected folder."));
                    return;
                }

                int pageNumber = 1;
                var pages = new List<PrintablePage>();

                foreach (var level2Dir in level2Dirs)
                {
                    var level2Pages = GenerateLevel2Section(level2Dir, TemplateName, ref pageNumber);
                    pages.AddRange(level2Pages);
                }

                int totalPages = pages.Count;
                foreach (var page in pages)
                {
                    page.TotalPages = totalPages;
                }

                form.Pages = pages;
            }
            catch (Exception ex)
            {
                form.Pages.Clear();
                form.Header = "Error";
                form.Pages.Add(CreateErrorPage($"Form generation failed: {ex.Message}\n\n{ex.StackTrace}"));
            }
        });

        return form;
    }

    protected virtual List<PrintablePage> GenerateLevel2Section(DirectoryInfo level2Dir, string pageHeader, ref int pageNumber)
    {
        var pages = new List<PrintablePage>();

        // Check if this is a special directory
        if (IsSpecialDirectory(level2Dir.Name))
        {
            pages = GenerateSpecialDirectory(level2Dir, pageHeader, ref pageNumber);
        }
        else
        {
            pages = GenerateNormalDirectory(level2Dir, pageHeader, ref pageNumber);
        }

        return pages;
    }

    /// <summary>
    /// Checks if a directory is special (like "Workstations").
    /// </summary>
    protected virtual bool IsSpecialDirectory(string dirName)
    {
        return SpecialDirectories.Any(special =>
            dirName.Equals(special, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Generates pages for special directories.
    /// Each Level 3 subdirectory gets its own page.
    /// </summary>
    protected virtual List<PrintablePage> GenerateSpecialDirectory(DirectoryInfo level2Dir, string pageHeader, ref int pageNumber)
    {
        var pages = new List<PrintablePage>();
        var allSubDirs = level2Dir.GetDirectories().OrderBy(d => d.Name).ToList();

        // Each subdirectory gets its own page
        int headerTypeIndex = 0;

        foreach (var subDir in allSubDirs)
        {
            RowType headerType = Level3HeaderTypes[headerTypeIndex % Level3HeaderTypes.Length];
            headerTypeIndex++;

            var level3Rows = new List<PrintableFormItem>();

            // Add Level 3 header
            level3Rows.Add(new PrintableFormItem
            {
                Content = subDir.Name,
                Type = headerType
            });

            // Get files from this subdirectory
            var subDirFiles = GetFileNamesFromDirectory(subDir);
            foreach (var file in subDirFiles)
            {
                level3Rows.Add(new PrintableFormItem
                {
                    Content = file,
                    Type = RowType.File
                });
            }

            // Fill remaining space with blank rows
            int blankRows = Math.Max(0, AvailableRowsPerPage - level3Rows.Count);
            for (int i = 0; i < blankRows; i++)
            {
                level3Rows.Add(new PrintableFormItem { Type = RowType.Blank });
            }

            // Create a page for this workstation
            var page = new PrintablePage
            {
                PageHeader = pageHeader,
                Level2Header = level2Dir.Name,
                PageNumber = pageNumber,
                Rows = level3Rows
            };

            pages.Add(page);
            pageNumber++;
        }

        return pages;
    }

    /// <summary>
    /// Generates pages for normal directories.
    /// All Level 3 sections fit on one or more pages with the same Level 2 header.
    /// </summary>
    protected virtual List<PrintablePage> GenerateNormalDirectory(DirectoryInfo level2Dir, string pageHeader, ref int pageNumber)
    {
        var pages = new List<PrintablePage>();
        var level2Rows = new List<PrintableFormItem>();

        var allSubDirs = level2Dir.GetDirectories().OrderBy(d => d.Name).ToList();

        if (allSubDirs.Count == 0)
        {
            // No subdirectories - add files directly from Level 2
            var filesInLevel2 = level2Dir.GetFiles()
                .Where(f => !f.Name.StartsWith("."))
                .OrderBy(f => f.Name)
                .ToList();

            foreach (var file in filesInLevel2)
            {
                level2Rows.Add(new PrintableFormItem
                {
                    Content = Path.GetFileNameWithoutExtension(file.Name),
                    Type = RowType.File
                });
            }
        }
        else
        {
            // Has subdirectories - process each as Level 3 section
            int headerTypeIndex = 0;

            foreach (var subDir in allSubDirs)
            {
                RowType headerType = Level3HeaderTypes[headerTypeIndex % Level3HeaderTypes.Length];
                headerTypeIndex++;

                // Add the subdirectory header
                level2Rows.Add(new PrintableFormItem
                {
                    Content = subDir.Name,
                    Type = headerType
                });

                // Get files from this subdirectory
                var subDirFiles = GetFileNamesFromDirectory(subDir);
                foreach (var file in subDirFiles)
                {
                    level2Rows.Add(new PrintableFormItem
                    {
                        Content = file,
                        Type = RowType.File
                    });
                }

                // Add blank rows to fill space for this section
                int blankRows = Math.Max(0, 12 - subDirFiles.Count);
                for (int i = 0; i < blankRows; i++)
                {
                    level2Rows.Add(new PrintableFormItem { Type = RowType.Blank });
                }
            }
        }

        pages = PaginateLevel2Section(level2Rows, pageHeader, level2Dir.Name, ref pageNumber);

        return pages;
    }

    /// <summary>
    /// Gets just the filenames from a directory (not file contents).
    /// </summary>
    protected virtual List<string> GetFileNamesFromDirectory(DirectoryInfo directory)
    {
        var fileNames = new List<string>();

        try
        {
            var allFiles = directory.GetFiles().OrderBy(f => f.Name);

            foreach (var file in allFiles)
            {
                // Just add the filename without extension
                fileNames.Add(Path.GetFileNameWithoutExtension(file.Name));
            }
        }
        catch (Exception ex)
        {
            fileNames.Add($"[Error reading directory: {ex.Message}]");
        }

        return fileNames;
    }

    protected virtual List<PrintablePage> PaginateLevel2Section(
        List<PrintableFormItem> level2Rows,
        string pageHeader,
        string level2Name,
        ref int pageNumber)
    {
        var pages = new List<PrintablePage>();
        int rowsPerPage = AvailableRowsPerPage;
        int currentRow = 0;
        bool isFirstPage = true;

        while (currentRow < level2Rows.Count || isFirstPage)
        {
            var page = new PrintablePage
            {
                PageHeader = pageHeader,
                Level2Header = level2Name,
                PageNumber = pageNumber,
                IsContinuationPage = !isFirstPage
            };

            int rowsToTake = Math.Min(rowsPerPage, level2Rows.Count - currentRow);
            page.Rows.AddRange(level2Rows.Skip(currentRow).Take(rowsToTake));

            int blankRowsNeeded = rowsPerPage - rowsToTake;
            for (int i = 0; i < blankRowsNeeded; i++)
            {
                page.Rows.Add(new PrintableFormItem { Type = RowType.Blank });
            }

            pages.Add(page);
            currentRow += rowsToTake;
            pageNumber++;
            isFirstPage = false;

            if (currentRow >= level2Rows.Count)
                break;
        }

        return pages;
    }

    protected virtual PrintablePage CreateErrorPage(string errorMessage)
    {
        return new PrintablePage
        {
            PageHeader = TemplateName,
            Level2Header = "Error",
            PageNumber = 1,
            TotalPages = 1,
            Rows = new List<PrintableFormItem>
            {
                new PrintableFormItem { Content = "An error occurred generating this form:", Type = RowType.Level2Header },
                new PrintableFormItem { Content = errorMessage, Type = RowType.File }
            }
        };
    }
}