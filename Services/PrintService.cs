using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using Windows.Storage.Pickers;
using WinRT;

namespace AIM.Services;

public class PrintService : IPrintService
{
    private PrintDocument? _printDocument;
    private IPrintDocumentSource? _printDocumentSource;
    private readonly List<UIElement> _pagesToPrint = new();
    private Transform? _originalTransform;
    private UIElement? _elementBeingPrinted;

    public async Task PrintAsync(UIElement elementToPrint, string jobTitle)
    {
        try
        {
            var mainWindow = App.MainWindow;
            if (mainWindow?.Content is not FrameworkElement rootElement)
            {
                throw new InvalidOperationException("Cannot access main window");
            }

            // Show print options dialog
            var dialog = new ContentDialog
            {
                Title = "Print Options",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Choose how to print your inventory form:",
                            FontSize = 14,
                            Margin = new Thickness(0, 0, 0, 16),
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        }
                    }
                },
                PrimaryButtonText = "Print to Printer",
                SecondaryButtonText = "Save as PDF",
                CloseButtonText = "Cancel",
                XamlRoot = rootElement.XamlRoot
            };

            var result = await dialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    await PrintToPrinterAsync(elementToPrint, jobTitle);
                    break;

                case ContentDialogResult.Secondary:
                    await SaveAsPdfAsync();
                    break;

                case ContentDialogResult.None:
                    // User cancelled
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Print error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Prints using PrintDocument for proper multi-page support.
    /// </summary>
    private async Task PrintToPrinterAsync(UIElement elementToPrint, string jobTitle)
    {
        try
        {
            var mainWindow = App.MainWindow;
            if (mainWindow == null)
                return;

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

            // Store reference to element being printed
            _elementBeingPrinted = elementToPrint;

            // Force layout update
            if (elementToPrint is FrameworkElement fElement)
            {
                fElement.UpdateLayout();
                System.Diagnostics.Debug.WriteLine($"Element size for printing: {fElement.ActualWidth} x {fElement.ActualHeight}");
            }

            // Setup print document
            _pagesToPrint.Clear();
            _pagesToPrint.Add(elementToPrint);

            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;

            // Register event handlers for printing
            _printDocument.GetPreviewPage += OnGetPreviewPage;
            _printDocument.AddPages += OnAddPages;

            // Get PrintManager using interop
            var printManager = PrintManagerInterop.GetForWindow(windowHandle);

            // Register for print task
            printManager.PrintTaskRequested += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("PrintTaskRequested fired");
                var printTask = args.Request.CreatePrintTask(jobTitle, sourceRequested =>
                {
                    sourceRequested.SetSource(_printDocumentSource);
                });

                printTask.Options.Orientation = PrintOrientation.Portrait;



                // Handle completion
                printTask.Completed += (task, completedArgs) =>
                {
                    System.Diagnostics.Debug.WriteLine("Print task completed");
                    RestoreOriginalTransform();
                };
            };

            // Show the print UI
            await PrintManagerInterop.ShowPrintUIForWindowAsync(windowHandle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Print error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            RestoreOriginalTransform();

            var mainWindow = App.MainWindow;
            if (mainWindow?.Content is FrameworkElement rootElement)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Print Error",
                    Content = $"Unable to open print dialog:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = rootElement.XamlRoot
                };
                _ = await errorDialog.ShowAsync();
            }
        }
    }

    /// <summary>
    /// Handles getting preview pages.
    /// </summary>
    private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnGetPreviewPage called for page {e.PageNumber}");

        try
        {
            if (e.PageNumber > 0 && e.PageNumber <= _pagesToPrint.Count)
            {
                _printDocument?.SetPreviewPage(e.PageNumber, _pagesToPrint[e.PageNumber - 1]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnGetPreviewPage: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles adding pages to the print document.
    /// </summary>
    private void OnAddPages(object sender, AddPagesEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnAddPages called, adding {_pagesToPrint.Count} pages");

        try
        {
            foreach (var page in _pagesToPrint)
            {
                _printDocument?.AddPage(page);
            }
            _printDocument?.AddPagesComplete();
            System.Diagnostics.Debug.WriteLine("All pages added");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAddPages: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores the original transform after printing.
    /// </summary>
    private void RestoreOriginalTransform()
    {
        try
        {
            if (_elementBeingPrinted is FrameworkElement content)
            {
                content.RenderTransform = _originalTransform;
                System.Diagnostics.Debug.WriteLine("Original transform restored");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring transform: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows instructions for saving as PDF.
    /// </summary>
    private async Task SaveAsPdfAsync()
    {
        try
        {
            var mainWindow = App.MainWindow;
            if (mainWindow == null)
                return;

            var savePicker = new FileSavePicker
            {
                SuggestedFileName = $"AIM_Inventory_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultFileExtension = ".pdf"
            };

            savePicker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

            var file = await savePicker.PickSaveFileAsync();
            if (file == null)
                return;

            if (mainWindow.Content is FrameworkElement rootElement)
            {
                var dialog = new ContentDialog
                {
                    Title = "Save as PDF",
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock { Text = "Location selected successfully!", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                            new TextBlock { Text = "Follow these steps:", FontSize = 13, Margin = new Thickness(0, 8, 0, 0) },
                            new TextBlock { Text = "1. Click 'Open Print Dialog' below", FontSize = 13 },
                            new TextBlock { Text = "2. Select 'Microsoft Print to PDF'", FontSize = 13 },
                            new TextBlock { Text = "3. Click Print", FontSize = 13 },
                        }
                    },
                    PrimaryButtonText = "Open Print Dialog",
                    CloseButtonText = "Cancel",
                    XamlRoot = rootElement.XamlRoot
                };

                var dialogResult = await dialog.ShowAsync();

                if (dialogResult == ContentDialogResult.Primary)
                {
                    await TriggerPrintDialogForPdfAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF save error: {ex.Message}");
        }
    }

    /// <summary>
    /// Triggers the print dialog for PDF saving.
    /// </summary>
    private async Task TriggerPrintDialogForPdfAsync()
    {
        try
        {
            var mainWindow = App.MainWindow;
            if (mainWindow == null)
                return;

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;

            _printDocument.Paginate += (s, e) =>
            {
                _printDocument.SetPreviewPageCount(1, PreviewPageCountType.Final);
            };

            _printDocument.GetPreviewPage += (s, e) =>
            {
                if (e.PageNumber == 1)
                {
                    _printDocument.SetPreviewPage(e.PageNumber, new TextBlock { Text = "Your form" });
                }
            };

            _printDocument.AddPages += (s, e) =>
            {
                _printDocument.AddPage(new TextBlock { Text = "Your form" });
                _printDocument.AddPagesComplete();
            };

            var printManager = PrintManagerInterop.GetForWindow(windowHandle);

            printManager.PrintTaskRequested += (sender, args) =>
            {
                var printTask = args.Request.CreatePrintTask("Save as PDF", sourceRequested =>
                {
                    sourceRequested.SetSource(_printDocumentSource);
                });
            };

            await PrintManagerInterop.ShowPrintUIForWindowAsync(windowHandle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF print dialog error: {ex.Message}");
        }
    }
}