using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Printing;

namespace AIM.Services;

public class PrintService : IPrintService
{
    private PrintDocument? _printDocument;
    private IPrintDocumentSource? _printDocumentSource;
    private readonly List<UIElement> _pagesToPrint = new();

    public async Task PrintAsync(UIElement elementToPrint, string jobTitle)
    {
        _pagesToPrint.Clear();

        if (elementToPrint is not ItemsControl itemsControl)
        {
            // Fallback for printing a single element that isn't an ItemsControl
            _pagesToPrint.Add(elementToPrint);
        }
        else
        {
            // This is a simplified way to get the generated containers.
            // This works reliably when virtualization is off, which is the default for a ListView in a ScrollViewer.
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                if (itemsControl.ContainerFromIndex(i) is UIElement container)
                {
                    _pagesToPrint.Add(container);
                }
            }
        }

        if (_pagesToPrint.Count == 0)
        {
            // Nothing to print, maybe show a dialog to the user?
            Console.WriteLine("No content was prepared for printing.");
            return;
        }

        _printDocument = new PrintDocument();
        _printDocumentSource = _printDocument.DocumentSource;
        _printDocument.Paginate += OnPaginate;
        _printDocument.GetPreviewPage += OnGetPreviewPage;
        _printDocument.AddPages += OnAddPages;

        var printManager = PrintManager.GetForCurrentView();
        printManager.PrintTaskRequested += OnPrintTaskRequested;

        try
        {
            await PrintManager.ShowPrintUIAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error showing print UI: {ex.Message}");
        }
        finally
        {
            printManager.PrintTaskRequested -= OnPrintTaskRequested;
        }
    }

    private void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        var printTask = args.Request.CreatePrintTask("AIM Inventory Form", sourceRequested =>
        {
            sourceRequested.SetSource(_printDocumentSource);
        });
    }

    private void OnPaginate(object sender, PaginateEventArgs e)
    {
        _printDocument?.SetPreviewPageCount(_pagesToPrint.Count, PreviewPageCountType.Final);
    }

    private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        if (e.PageNumber > 0 && e.PageNumber <= _pagesToPrint.Count)
        {
            _printDocument?.SetPreviewPage(e.PageNumber, _pagesToPrint[e.PageNumber - 1]);
        }
    }

    private void OnAddPages(object sender, AddPagesEventArgs e)
    {
        foreach (var page in _pagesToPrint)
        {
            _printDocument?.AddPage(page);
        }
        _printDocument?.AddPagesComplete();
    }
}