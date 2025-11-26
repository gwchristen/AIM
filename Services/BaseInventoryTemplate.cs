using AIM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIM.Services;

public abstract class BaseInventoryTemplate : IFormTemplate
{
    private readonly IPrintPaginationService _paginationService;

    public abstract string TemplateName { get; }

    protected virtual RowType[] Level3HeaderTypes => new[]
    {
        RowType.Level3Header_A,
        RowType.Level3Header_B,
        RowType.Level3Header_C
    };

    // Constructor with dependency injection
    protected BaseInventoryTemplate(IPrintPaginationService paginationService)
    {
        _paginationService = paginationService;
    }

    // Parameterless constructor for backward compatibility
    protected BaseInventoryTemplate() : this(new PrintPaginationService())
    {
    }

    public async Task<PrintableForm> GenerateAsync(string directoryPath)
    {
        var form = new PrintableForm
        {
            Header = TemplateName,
            SubHeader = "Inventory Summary"
        };

        // Collect all items as a flat list first
        var allItems = new List<PrintableFormItem>();

        await Task.Run(() =>
        {
            try
            {
                var opCoDirInfo = new DirectoryInfo(directoryPath);
                form.Header = opCoDirInfo.Name;

                var level2Dirs = opCoDirInfo.GetDirectories().OrderBy(d => d.Name).ToList();

                if (!level2Dirs.Any())
                {
                    allItems.Add(new PrintableFormItem
                    {
                        Content = "No subdirectories found in the selected folder.",
                        Type = RowType.Level3Header_C
                    });
                    return;
                }

                foreach (var level2Dir in level2Dirs)
                {
                    // Add Level 2 header
                    allItems.Add(new PrintableFormItem
                    {
                        Content = level2Dir.Name,
                        Type = RowType.Level2Header
                    });

                    // Get all subdirectories (Level 3)
                    var level3Dirs = level2Dir.GetDirectories().OrderBy(d => d.Name).ToList();

                    if (level3Dirs.Count == 0)
                    {
                        // No subdirectories - add files directly from Level 2
                        var filesInLevel2 = level2Dir.GetFiles()
                            .Where(f => !f.Name.StartsWith("."))
                            .OrderBy(f => f.Name)
                            .ToList();

                        foreach (var file in filesInLevel2)
                        {
                            allItems.Add(new PrintableFormItem
                            {
                                Content = Path.GetFileNameWithoutExtension(file.Name),
                                Type = RowType.File
                            });
                        }
                    }
                    else
                    {
                        // Process Level 3 subdirectories
                        int headerTypeIndex = 0;

                        foreach (var level3Dir in level3Dirs)
                        {
                            RowType headerType = Level3HeaderTypes[headerTypeIndex % Level3HeaderTypes.Length];
                            headerTypeIndex++;

                            // Add Level 3 header
                            allItems.Add(new PrintableFormItem
                            {
                                Content = level3Dir.Name,
                                Type = headerType
                            });

                            // Add files from this Level 3 directory
                            var files = level3Dir.GetFiles()
                                .Where(f => !f.Name.StartsWith("."))
                                .OrderBy(f => f.Name)
                                .ToList();

                            foreach (var file in files)
                            {
                                allItems.Add(new PrintableFormItem
                                {
                                    Content = Path.GetFileNameWithoutExtension(file.Name),
                                    Type = RowType.File
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                allItems.Clear();
                allItems.Add(new PrintableFormItem
                {
                    Content = $"Form generation failed: {ex.Message}",
                    Type = RowType.Level3Header_C
                });
            }
        });

        // Use the pagination service to create properly paginated pages
        form.Pages = _paginationService.PaginateContent(form.Header, allItems);

        return form;
    }
}