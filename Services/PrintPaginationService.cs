using AIM.Models;
using System;
using System.Collections.Generic;

namespace AIM.Services;

public class PrintPaginationService : IPrintPaginationService
{
    private const double PageHeight = 1056;
    private const double TopMargin = 40;
    private const double BottomMargin = 40;
    private const double HeaderHeight = 55;
    private const double Level2HeaderHeight = 30;
    private const double Level3HeaderHeight = 20;
    private const double FileRowHeight = 18;
    private const double BlankRowHeight = 18;
    private const double FooterHeight = 35;

    public static List<string> PaginationLog { get; } = new List<string>();

    private static void Log(string message)
    {
        PaginationLog.Add(message);
        Console.WriteLine(message);  // This might show in Output
        System.Diagnostics.Debug.WriteLine(message);
        System.Diagnostics.Trace.WriteLine(message);
    }

    private double GetAvailableContentHeight(bool hasLevel2Header)
    {
        double usedHeight = TopMargin + BottomMargin + HeaderHeight + FooterHeight;
        if (hasLevel2Header)
        {
            usedHeight += Level2HeaderHeight;
        }
        return PageHeight - usedHeight;
    }

    private double GetRowHeight(PrintableFormItem item)
    {
        return item.Type switch
        {
            RowType.Level2Header => Level2HeaderHeight,
            RowType.Level3Header_A => Level3HeaderHeight,
            RowType.Level3Header_B => Level3HeaderHeight,
            RowType.Level3Header_C => Level3HeaderHeight,
            RowType.File => FileRowHeight,
            RowType.Blank => BlankRowHeight,
            _ => FileRowHeight
        };
    }

    private bool IsLevel3Header(RowType type)
    {
        return type == RowType.Level3Header_A ||
               type == RowType.Level3Header_B ||
               type == RowType.Level3Header_C;
    }

    private bool IsHeader(RowType type)
    {
        return type == RowType.Level2Header || IsLevel3Header(type);
    }

    /// <summary>
    /// Counts how many File rows follow a Level3 header before the next header.
    /// </summary>
    private int CountFilesInSection(List<PrintableFormItem> allRows, int headerIndex)
    {
        int count = 0;
        for (int j = headerIndex + 1; j < allRows.Count; j++)
        {
            if (IsHeader(allRows[j].Type))
                break;
            if (allRows[j].Type == RowType.File)
                count++;
        }
        return count;
    }

    public List<PrintablePage> PaginateContent(string pageHeader, List<PrintableFormItem> allRows)
    {
        PaginationLog.Clear();

        double availableHeight = GetAvailableContentHeight(true);
        Log($"=== PAGINATION START ===");
        Log($"Available height per page: {availableHeight}px");
        Log($"Level3HeaderHeight: {Level3HeaderHeight}px, FileRowHeight: {FileRowHeight}px");
        Log($"Total input rows: {allRows.Count}");

        var pages = new List<PrintablePage>();
        var currentPageRows = new List<PrintableFormItem>();
        double currentPageUsedHeight = 0;
        string currentLevel2Header = string.Empty;
        bool isFirstPageForLevel2 = true;

        int i = 0;
        while (i < allRows.Count)
        {
            var row = allRows[i];

            // Skip blanks
            if (row.Type == RowType.Blank)
            {
                i++;
                continue;
            }

            // Level 2 header - start fresh
            if (row.Type == RowType.Level2Header)
            {
                if (currentPageRows.Count > 0)
                {
                    FillWithBlankRows(currentPageRows, availableHeight - currentPageUsedHeight);
                    pages.Add(CreatePage(pageHeader, currentLevel2Header, currentPageRows, !isFirstPageForLevel2));
                    currentPageRows = new List<PrintableFormItem>();
                    currentPageUsedHeight = 0;
                }
                currentLevel2Header = row.Content;
                isFirstPageForLevel2 = true;
                Log($"--- Level2: '{currentLevel2Header}' ---");
                i++;
                continue;
            }

            // Level 3 header
            if (IsLevel3Header(row.Type))
            {
                int fileCount = CountFilesInSection(allRows, i);
                double sectionHeight = Level3HeaderHeight + (fileCount * FileRowHeight);
                double remainingOnPage = availableHeight - currentPageUsedHeight;

                Log($"Level3 '{row.Content}': {fileCount} files, sectionHeight={sectionHeight}px, remaining={remainingOnPage}px, availableHeight={availableHeight}px");

                bool fitsOnCurrentPage = sectionHeight <= remainingOnPage;
                bool fitsOnFreshPage = sectionHeight <= availableHeight;

                Log($"  fitsOnCurrentPage={fitsOnCurrentPage}, fitsOnFreshPage={fitsOnFreshPage}");

                if (fitsOnCurrentPage)
                {
                    Log($"  -> Adding to current page");
                }
                else if (fitsOnFreshPage)
                {
                    Log($"  -> Moving to new page");
                    if (currentPageRows.Count > 0)
                    {
                        FillWithBlankRows(currentPageRows, availableHeight - currentPageUsedHeight);
                        pages.Add(CreatePage(pageHeader, currentLevel2Header, currentPageRows, !isFirstPageForLevel2));
                        isFirstPageForLevel2 = false;
                    }
                    currentPageRows = new List<PrintableFormItem>();
                    currentPageUsedHeight = 0;
                }
                else
                {
                    Log($"  -> Section too large, will split");
                    if (currentPageRows.Count > 0)
                    {
                        FillWithBlankRows(currentPageRows, availableHeight - currentPageUsedHeight);
                        pages.Add(CreatePage(pageHeader, currentLevel2Header, currentPageRows, !isFirstPageForLevel2));
                        isFirstPageForLevel2 = false;
                    }
                    currentPageRows = new List<PrintableFormItem>();
                    currentPageUsedHeight = 0;
                }

                // Add the Level3 header
                currentPageRows.Add(row);
                currentPageUsedHeight += Level3HeaderHeight;
                Log($"  Added header, usedHeight={currentPageUsedHeight}px");
                i++;

                // Add all files in this section
                int filesAdded = 0;
                while (i < allRows.Count && !IsHeader(allRows[i].Type))
                {
                    var fileRow = allRows[i];

                    if (fileRow.Type == RowType.Blank)
                    {
                        i++;
                        continue;
                    }

                    double fileHeight = GetRowHeight(fileRow);

                    // Check if we need a new page
                    if (currentPageUsedHeight + fileHeight > availableHeight)
                    {
                        Log($"  Page full at {filesAdded} files, starting new page");
                        FillWithBlankRows(currentPageRows, availableHeight - currentPageUsedHeight);
                        pages.Add(CreatePage(pageHeader, currentLevel2Header, currentPageRows, !isFirstPageForLevel2));
                        isFirstPageForLevel2 = false;
                        currentPageRows = new List<PrintableFormItem>();
                        currentPageUsedHeight = 0;
                    }

                    currentPageRows.Add(fileRow);
                    currentPageUsedHeight += fileHeight;
                    filesAdded++;
                    i++;
                }
                Log($"  Finished section, added {filesAdded} files total");
                continue;
            }

            // Regular file row (not under a Level3 header)
            double rowHeight = GetRowHeight(row);
            if (currentPageUsedHeight + rowHeight > availableHeight)
            {
                FillWithBlankRows(currentPageRows, availableHeight - currentPageUsedHeight);
                pages.Add(CreatePage(pageHeader, currentLevel2Header, currentPageRows, !isFirstPageForLevel2));
                isFirstPageForLevel2 = false;
                currentPageRows = new List<PrintableFormItem>();
                currentPageUsedHeight = 0;
            }
            currentPageRows.Add(row);
            currentPageUsedHeight += rowHeight;
            i++;
        }

        // Final page
        if (currentPageRows.Count > 0)
        {
            FillWithBlankRows(currentPageRows, availableHeight - currentPageUsedHeight);
            pages.Add(CreatePage(pageHeader, currentLevel2Header, currentPageRows, !isFirstPageForLevel2));
        }

        // Set page numbers
        for (int p = 0; p < pages.Count; p++)
        {
            pages[p].PageNumber = p + 1;
            pages[p].TotalPages = pages.Count;
        }

        Log($"=== PAGINATION END: {pages.Count} pages ===");
        return pages;
    }

    private void FillWithBlankRows(List<PrintableFormItem> rows, double remainingHeight)
    {
        while (remainingHeight >= BlankRowHeight)
        {
            rows.Add(new PrintableFormItem { Type = RowType.Blank, Content = string.Empty });
            remainingHeight -= BlankRowHeight;
        }
    }

    private PrintablePage CreatePage(string pageHeader, string level2Header, List<PrintableFormItem> rows, bool isContinuationPage)
    {
        Log($"  >> Created page with {rows.Count} rows");
        return new PrintablePage
        {
            PageHeader = pageHeader,
            Level2Header = level2Header,
            Rows = new List<PrintableFormItem>(rows),
            IsContinuationPage = isContinuationPage
        };
    }
}