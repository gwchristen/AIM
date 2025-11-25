using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Graphics.Printing;

namespace AIM.Services;

/// <summary>
/// Custom IPrintDocumentSource implementation for multi-page printing in WinUI 3.
/// </summary>
public class MultiPagePrintDocumentSource : Windows.Graphics.Printing.IPrintDocumentSource
{
    private readonly List<UIElement> _pages;
    private Transform? _originalTransform;
    private UIElement? _currentElement;

    public event EventHandler<object>? PrintTaskOptionsChanged;

    public MultiPagePrintDocumentSource(List<UIElement> pages)
    {
        _pages = pages ?? throw new ArgumentNullException(nameof(pages));
    }

    /// <summary>
    /// Applies scaling to fit content on page.
    /// </summary>
    public void ApplyScaling(UIElement element, PrintPageDescription pageDescription)
    {
        if (element is not FrameworkElement fElement)
            return;

        _currentElement = element;
        _originalTransform = fElement.RenderTransform;

        // Get page dimensions with standard margins (0.5 inch = 48 pixels at 96 DPI)
        double marginLeft = 48;
        double marginTop = 48;
        double marginRight = 48;
        double marginBottom = 48;

        double printableWidth = pageDescription.PageSize.Width - marginLeft - marginRight;
        double printableHeight = pageDescription.PageSize.Height - marginTop - marginBottom;

        System.Diagnostics.Debug.WriteLine($"Page size: {pageDescription.PageSize.Width} x {pageDescription.PageSize.Height}");
        System.Diagnostics.Debug.WriteLine($"Printable area: {printableWidth} x {printableHeight}");

        // Get content dimensions
        double contentWidth = fElement.ActualWidth;
        double contentHeight = fElement.ActualHeight;

        System.Diagnostics.Debug.WriteLine($"Content size: {contentWidth} x {contentHeight}");

        // Calculate scale to fit content
        double scaleX = printableWidth / contentWidth;
        double scaleY = printableHeight / contentHeight;
        double scale = Math.Min(scaleX, scaleY);

        System.Diagnostics.Debug.WriteLine($"Calculated scale: {scale}");

        // Apply scale if needed
        if (scale < 1.0)
        {
            fElement.RenderTransform = new ScaleTransform
            {
                ScaleX = scale,
                ScaleY = scale,
                CenterX = 0,
                CenterY = 0
            };
            System.Diagnostics.Debug.WriteLine($"Applied scale transform: {scale}");
        }
    }

    /// <summary>
    /// Restores original transform after printing.
    /// </summary>
    public void RestoreOriginalTransform()
    {
        try
        {
            if (_currentElement is FrameworkElement fElement)
            {
                fElement.RenderTransform = _originalTransform;
                System.Diagnostics.Debug.WriteLine("Original transform restored");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring transform: {ex.Message}");
        }
    }

    public IAsyncAction GetPreviewPageAsync(PrintPageDescription printPageDescription, uint pageNumber)
    {
        throw new NotImplementedException();
    }

    public IAsyncAction GetPrintPageAsync(PrintPageDescription printPageDescription, uint pageNumber)
    {
        throw new NotImplementedException();
    }

    public IAsyncOperation<uint> GetPreviewPageCountAsync(PrintPageDescription printPageDescription)
    {
        throw new NotImplementedException();
    }

    public IAsyncOperation<uint> GetPrintPageCountAsync(PrintPageDescription printPageDescription)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        RestoreOriginalTransform();
    }
}