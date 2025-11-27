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
    private const double Level2HeaderHeight = 30;   // Keep same
    private const double Level3HeaderHeight = 24;   // Slightly taller due to padding
    private const double FileRowHeight = 20;        // XAML has MinHeight="20"
    private const double BlankRowHeight = 20;       // XAML has MinHeight="20"
    private const double FooterHeight = 35;

    public static List<string> PaginationLog { get; } = new List<string>();

    private static void Log(string message)
    {
        PaginationLog.Add(message);
        System.Diagnostics.Debug.WriteLine(message);
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

    public List<PrintablePage> PaginateContent(string pageHeader, List<PrintableFormItem> allRows)
    {
        PaginationLog.Clear();

        double availableHeight = GetAvailableContentHeight(true);
        Log($"=== PAGINATION START ===");
        Log($"Available height per page: {availableHeight}px");
        Log($"Max rows at {FileRowHeight}px each: {(int)(availableHeight / FileRowHeight)}");
        Log($"Total input rows: {allRows.Count}");

        // Log all input rows for debugging
        Log($"=== INPUT ROWS ===");
        for (int idx = 0; idx < allRows.Count; idx++)
        {
            var r = allRows[idx];
            Log($"  [{idx}] {r.Type}: {r.Content}");
        }
        Log($"=== END INPUT ROWS ===");

        var pages = new List<PrintablePage>();
        var currentPageRows = new List<PrintableFormItem>();
        double currentPageUsedHeight = 0;
        string currentLevel2Header = string.Empty;
        string currentLevel3Header = string.Empty;
        RowType currentLevel3HeaderType = RowType.Level3Header_A;
        bool isFirstPageForLevel2 = true;
        bool needsContinuationHeader = false;
        int pageNumber = 1;

        int i = 0;
        while (i < allRows.Count)
        {
            var row = allRows[i];
            Log($"[i={i}] Processing: {row.Type} '{row.Content}' | PageRows={currentPageRows.Count}, UsedHeight={currentPageUsedHeight}, NeedsCont={needsContinuationHeader}, CurrL3='{currentLevel3Header}'");

            // Skip blanks from input
            if (row.Type == RowType.Blank)
            {
                Log($"  -> Skipping blank");
                i++;
                continue;
            }

            double rowHeight = GetRowHeight(row);

            // Level 2 header - start fresh page
            if (row.Type == RowType.Level2Header)
            {
                if (currentPageRows.Count > 0)
                {
                    Log($"  -> Finalizing page {pageNumber} before Level2 header");
                    var filledPage = DistributeBlankRowsEvenly(currentPageRows, availableHeight, currentPageUsedHeight);
                    pages.Add(CreatePage(pageHeader, currentLevel2Header, filledPage, !isFirstPageForLevel2));
                    pageNumber++;
                    currentPageRows = new List<PrintableFormItem>();
                    currentPageUsedHeight = 0;
                }
                currentLevel2Header = row.Content;
                currentLevel3Header = string.Empty;
                needsContinuationHeader = false;
                isFirstPageForLevel2 = true;
                Log($"  -> Set Level2 header to '{currentLevel2Header}'");
                i++;
                continue;
            }

            // Level 3 header
            if (IsLevel3Header(row.Type))
            {
                // Check if header fits on current page
                if (currentPageUsedHeight + rowHeight > availableHeight)
                {
                    Log($"  -> Page full before Level3 header, finalizing page {pageNumber}");
                    var filledPage = DistributeBlankRowsEvenly(currentPageRows, availableHeight, currentPageUsedHeight);
                    pages.Add(CreatePage(pageHeader, currentLevel2Header, filledPage, !isFirstPageForLevel2));
                    pageNumber++;
                    isFirstPageForLevel2 = false;
                    currentPageRows = new List<PrintableFormItem>();
                    currentPageUsedHeight = 0;
                }

                // New section - not a continuation
                currentLevel3Header = row.Content;
                currentLevel3HeaderType = row.Type;
                needsContinuationHeader = false;

                currentPageRows.Add(row);
                currentPageUsedHeight += rowHeight;
                Log($"  -> Added Level3 header '{row.Content}', UsedHeight now {currentPageUsedHeight}");
                i++;
                continue;
            }

            // File row
            if (row.Type == RowType.File)
            {
                // Calculate total height needed for this file
                double heightNeeded = rowHeight;

                // If we need a continuation header, include that in the height calculation
                if (needsContinuationHeader && !string.IsNullOrEmpty(currentLevel3Header))
                {
                    heightNeeded += Level3HeaderHeight;
                    Log($"  -> Will need continuation header, heightNeeded={heightNeeded}");
                }

                // Check if everything fits on current page
                if (currentPageUsedHeight + heightNeeded > availableHeight)
                {
                    Log($"  -> PAGE FULL: usedHeight={currentPageUsedHeight} + needed={heightNeeded} > available={availableHeight}");
                    Log($"  -> Finalizing page {pageNumber} with {currentPageRows.Count} rows");

                    // Log what's on this page before finalizing
                    Log($"  -> Page {pageNumber} contents:");
                    foreach (var pr in currentPageRows)
                    {
                        Log($"      {pr.Type}: {pr.Content}");
                    }

                    var filledPage = DistributeBlankRowsEvenly(currentPageRows, availableHeight, currentPageUsedHeight);
                    pages.Add(CreatePage(pageHeader, currentLevel2Header, filledPage, !isFirstPageForLevel2));
                    pageNumber++;
                    isFirstPageForLevel2 = false;
                    currentPageRows = new List<PrintableFormItem>();
                    currentPageUsedHeight = 0;

                    // We're starting a new page mid-section, so we need continuation header
                    needsContinuationHeader = true;
                    Log($"  -> Set needsContinuationHeader=true, currentLevel3Header='{currentLevel3Header}'");

                    // Don't increment i - reprocess this file on the new page
                    continue;
                }

                // Add continuation header if needed (now we know it fits)
                if (needsContinuationHeader && !string.IsNullOrEmpty(currentLevel3Header))
                {
                    Log($"  -> Adding continuation header '{currentLevel3Header} (cont. )'");
                    currentPageRows.Add(new PrintableFormItem
                    {
                        Content = currentLevel3Header + " (cont.)",
                        Type = currentLevel3HeaderType
                    });
                    currentPageUsedHeight += Level3HeaderHeight;
                    needsContinuationHeader = false;
                }

                // Add the file
                currentPageRows.Add(row);
                currentPageUsedHeight += rowHeight;
                Log($"  -> Added file '{row.Content}', UsedHeight now {currentPageUsedHeight}");
                i++;
                continue;
            }

            // Any other row type
            if (currentPageUsedHeight + rowHeight > availableHeight)
            {
                var filledPage = DistributeBlankRowsEvenly(currentPageRows, availableHeight, currentPageUsedHeight);
                pages.Add(CreatePage(pageHeader, currentLevel2Header, filledPage, !isFirstPageForLevel2));
                pageNumber++;
                isFirstPageForLevel2 = false;
                currentPageRows = new List<PrintableFormItem>();
                currentPageUsedHeight = 0;
                continue; // Reprocess this row
            }
            currentPageRows.Add(row);
            currentPageUsedHeight += rowHeight;
            i++;
        }

        // Final page
        if (currentPageRows.Count > 0)
        {
            Log($"  -> Finalizing last page {pageNumber} with {currentPageRows.Count} rows");
            var filledPage = DistributeBlankRowsEvenly(currentPageRows, availableHeight, currentPageUsedHeight);
            pages.Add(CreatePage(pageHeader, currentLevel2Header, filledPage, !isFirstPageForLevel2));
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

    private List<PrintableFormItem> DistributeBlankRowsEvenly(List<PrintableFormItem> rows, double availableHeight, double usedHeight)
    {
        double remainingHeight = availableHeight - usedHeight;
        int totalBlanks = (int)(remainingHeight / BlankRowHeight);

        Log($"  DistributeBlankRowsEvenly: usedHeight={usedHeight}, remainingHeight={remainingHeight}, totalBlanks={totalBlanks}");

        if (totalBlanks <= 0)
        {
            Log($"  No space for blanks");
            return new List<PrintableFormItem>(rows);
        }

        // Find insertion points (end of each section)
        var insertionPoints = new List<int>();

        for (int i = 0; i < rows.Count; i++)
        {
            bool isLastRow = (i == rows.Count - 1);
            bool nextIsLevel3Header = !isLastRow && IsLevel3Header(rows[i + 1].Type);

            if (rows[i].Type == RowType.File && (isLastRow || nextIsLevel3Header))
            {
                insertionPoints.Add(i);
                Log($"    Insertion point at index {i}: '{rows[i].Content}'");
            }
            else if (IsLevel3Header(rows[i].Type) && (isLastRow || nextIsLevel3Header))
            {
                insertionPoints.Add(i);
                Log($"    Insertion point (empty section) at index {i}: '{rows[i].Content}'");
            }
        }

        if (insertionPoints.Count == 0)
        {
            insertionPoints.Add(rows.Count - 1);
            Log($"    No sections found, inserting at end");
        }

        int sectionCount = insertionPoints.Count;
        Log($"  Sections: {sectionCount}");

        // Only add blanks if we have at least 1 per section
        if (totalBlanks < sectionCount)
        {
            Log($"  Not enough blanks ({totalBlanks}) for {sectionCount} sections - skipping");
            return new List<PrintableFormItem>(rows);
        }

        int blanksPerSection = totalBlanks / sectionCount;
        int extraBlanks = totalBlanks % sectionCount;

        Log($"  Distribution: {blanksPerSection} each, +{extraBlanks} to last");

        var result = new List<PrintableFormItem>();
        int insertionIndex = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            result.Add(rows[i]);

            if (insertionIndex < insertionPoints.Count && i == insertionPoints[insertionIndex])
            {
                int blanksToAdd = blanksPerSection;

                // Extra blanks go to last section
                if (insertionIndex == sectionCount - 1)
                {
                    blanksToAdd += extraBlanks;
                }

                Log($"  +{blanksToAdd} blanks after '{rows[i].Content}'");

                for (int b = 0; b < blanksToAdd; b++)
                {
                    result.Add(new PrintableFormItem { Type = RowType.Blank, Content = string.Empty });
                }

                insertionIndex++;
            }
        }

        return result;
    }

    private PrintablePage CreatePage(string pageHeader, string level2Header, List<PrintableFormItem> rows, bool isContinuationPage)
    {
        Log($"  >> Page created: {rows.Count} rows, L2='{level2Header}', continuation={isContinuationPage}");
        return new PrintablePage
        {
            PageHeader = pageHeader,
            Level2Header = level2Header,
            Rows = new List<PrintableFormItem>(rows),
            IsContinuationPage = isContinuationPage
        };
    }
}