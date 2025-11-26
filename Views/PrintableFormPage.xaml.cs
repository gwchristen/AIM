using AIM.Models;
using AIM.Services;
using AIM.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using Windows.Graphics.Printing;
using WinRT;

namespace AIM.Views;

public sealed partial class PrintableFormPage : Page
{
    private PrintManager? _printManager;
    private PrintDocument? _printDocument;
    private IPrintDocumentSource? _printDocumentSource;
    private readonly List<FrameworkElement> _printPages = new();
    private PrintableFormViewModel? _viewModel;

    public PrintableFormPage()
    {
        this.InitializeComponent();
        _viewModel = new PrintableFormViewModel(App.GetService<INavigationService>(), App.GetService<IPrintService>());
        this.DataContext = _viewModel;

        // Register this page with the ViewModel for printing
        _viewModel.SetPrintPage(this);

        System.Diagnostics.Debug.WriteLine("PrintableFormPage constructor completed");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is PrintableForm form)
        {
            _viewModel?.LoadFormData(form);
        }

        // Register for printing when page loads
        RegisterForPrinting();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        // Unregister from printing when page unloads
        UnregisterFromPrinting();
    }

    /// <summary>
    /// Registers the page for printing.
    /// </summary>
    private void RegisterForPrinting()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== RegisterForPrinting Started ===");

            // Get window handle first
            var mainWindow = App.MainWindow;
            if (mainWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: MainWindow is null");
                return;
            }

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            System.Diagnostics.Debug.WriteLine($"Window handle obtained: {windowHandle}");

            // Use PrintManagerInterop for WinUI 3
            _printManager = PrintManagerInterop.GetForWindow(windowHandle);
            System.Diagnostics.Debug.WriteLine("PrintManager obtained via PrintManagerInterop");

            _printManager.PrintTaskRequested += OnPrintTaskRequested;
            System.Diagnostics.Debug.WriteLine("PrintTaskRequested handler registered");

            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;
            System.Diagnostics.Debug.WriteLine("PrintDocument and DocumentSource created");

            _printDocument.Paginate += OnPaginate;
            _printDocument.GetPreviewPage += OnGetPreviewPage;
            _printDocument.AddPages += OnAddPages;
            System.Diagnostics.Debug.WriteLine("Print event handlers registered");

            System.Diagnostics.Debug.WriteLine("=== RegisterForPrinting Completed Successfully ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR in RegisterForPrinting ===");
            System.Diagnostics.Debug.WriteLine($"Exception: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Unregisters the page from printing.
    /// </summary>
    private void UnregisterFromPrinting()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== UnregisterFromPrinting Started ===");

            if (_printManager != null)
            {
                _printManager.PrintTaskRequested -= OnPrintTaskRequested;
                System.Diagnostics.Debug.WriteLine("PrintTaskRequested handler unregistered");
            }

            if (_printDocument != null)
            {
                _printDocument.Paginate -= OnPaginate;
                _printDocument.GetPreviewPage -= OnGetPreviewPage;
                _printDocument.AddPages -= OnAddPages;
                System.Diagnostics.Debug.WriteLine("Print event handlers unregistered");
            }

            System.Diagnostics.Debug.WriteLine("=== UnregisterFromPrinting Completed ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in UnregisterFromPrinting: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the print dialog - called from ViewModel command.
    /// </summary>
    public async void ShowPrintDialog()  // Make sure this is PUBLIC
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== ShowPrintDialog Started ===");

            var mainWindow = App.MainWindow;
            if (mainWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: MainWindow is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine("MainWindow obtained");

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            System.Diagnostics.Debug.WriteLine($"Window handle obtained: {windowHandle}");

            System.Diagnostics.Debug.WriteLine("About to call ShowPrintUIForWindowAsync");
            await PrintManagerInterop.ShowPrintUIForWindowAsync(windowHandle);
            System.Diagnostics.Debug.WriteLine("=== Print dialog should now be visible ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR in ShowPrintDialog ===");
            System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            // Show error dialog to user
            var errorDialog = new ContentDialog
            {
                Title = "Print Error",
                Content = $"Error showing print dialog:\n{ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            _ = await errorDialog.ShowAsync();
        }
    }

    /// <summary>
    /// Handles the PrintTaskRequested event.
    /// </summary>
    private void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== OnPrintTaskRequested fired ===");

            var printTask = args.Request.CreatePrintTask("AIM Inventory Form", sourceRequested =>
            {
                System.Diagnostics.Debug.WriteLine("PrintTaskSourceRequestedHandler invoked");
                sourceRequested.SetSource(_printDocumentSource);
                System.Diagnostics.Debug.WriteLine("PrintDocumentSource set");
            });

            System.Diagnostics.Debug.WriteLine("PrintTask created");
            printTask.Options.Orientation = PrintOrientation.Portrait;
            System.Diagnostics.Debug.WriteLine("Orientation set to Portrait");

            // Handle print task completion
            printTask.Completed += (task, completedArgs) =>
            {
                System.Diagnostics.Debug.WriteLine($"=== Print task completed ===");
                System.Diagnostics.Debug.WriteLine($"Completion status: {completedArgs.Completion}");
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR in OnPrintTaskRequested ===");
            System.Diagnostics.Debug.WriteLine($"Exception: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Handles the Paginate event - calculates total page count.
    /// </summary>
    private void OnPaginate(object sender, PaginateEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== OnPaginate called ===");

            // Build print pages from FlipView items
            BuildPrintPages();

            // Set the total page count
            _printDocument?.SetPreviewPageCount(_printPages.Count, PreviewPageCountType.Final);

            System.Diagnostics.Debug.WriteLine($"Preview page count set to: {_printPages.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR in OnPaginate ===");
            System.Diagnostics.Debug.WriteLine($"Exception: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Handles the GetPreviewPage event - provides preview for each page.
    /// </summary>
    private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"OnGetPreviewPage called for page {e.PageNumber}");

            if (e.PageNumber > 0 && e.PageNumber <= _printPages.Count)
            {
                _printDocument?.SetPreviewPage(e.PageNumber, _printPages[e.PageNumber - 1]);
                System.Diagnostics.Debug.WriteLine($"Preview page {e.PageNumber} set");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Page number {e.PageNumber} out of range (total: {_printPages.Count})");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in OnGetPreviewPage: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the AddPages event - adds all pages to the print job.
    /// </summary>
    private void OnAddPages(object sender, AddPagesEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== OnAddPages called, adding {_printPages.Count} pages ===");

            foreach (var page in _printPages)
            {
                _printDocument?.AddPage(page);
            }

            _printDocument?.AddPagesComplete();

            System.Diagnostics.Debug.WriteLine($"=== All {_printPages.Count} pages added to print job ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in OnAddPages: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Builds the print pages from the FlipView's data source.
    /// </summary>
    private void BuildPrintPages()
    {
        _printPages.Clear();

        try
        {
            System.Diagnostics.Debug.WriteLine("=== BuildPrintPages started ===");

            if (_viewModel?.Pages == null || _viewModel.Pages.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("WARNING: No pages to print");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Building print pages from {_viewModel.Pages.Count} FlipView items");

            foreach (var page in _viewModel.Pages)
            {
                // Create a visual representation of each page
                var pageElement = CreatePrintPageVisual(page);
                _printPages.Add(pageElement);
                System.Diagnostics.Debug.WriteLine($"Added print page {_printPages.Count}");
            }

            System.Diagnostics.Debug.WriteLine($"=== BuildPrintPages completed: {_printPages.Count} pages built ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR in BuildPrintPages ===");
            System.Diagnostics.Debug.WriteLine($"Exception: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private FrameworkElement CreatePrintPageVisual(PrintablePage page)
    {
        // Create the outer container with margins
        var outerGrid = new Grid
        {
            Margin = new Thickness(40), // 0. 5 inch margins (40 units ≈ 0.5 inch at 96 DPI)
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
        };

        // Create the page container
        var pageGrid = new Grid
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            Margin = new Thickness(0),
            Padding = new Thickness(0)
        };

        pageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pageGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // ...  (rest of the header, level2, content, and footer code stays the same) ...

        outerGrid.Children.Add(pageGrid);

        // Force layout with standard letter size MINUS margins
        // Letter is 850x1100, minus 40 on each side = 770x1020
        pageGrid.Measure(new Windows.Foundation.Size(770, 1020));
        pageGrid.Arrange(new Windows.Foundation.Rect(0, 0, 770, 1020));

        return outerGrid;
    }

    /// <summary>
    /// Creates a visual representation of a single row item.
    /// </summary>
    private FrameworkElement CreatePrintRowVisual(PrintableFormItem item)
    {
        var rowGrid = new Grid
        {
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
            BorderThickness = new Thickness(1),
            MinHeight = 20
        };

        // Define columns (5-column layout)
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

        // Add column separators (these go on top of content)
        for (int i = 0; i < 4; i++)
        {
            var border = new Border
            {
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            Grid.SetColumn(border, i);
            rowGrid.Children.Add(border);
        }

        // Determine background color based on RowType (matches FormRowTemplateSelector logic)
        Windows.UI.Color backgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255); // White default
        bool isHeader = false;
        bool isBold = false;

        switch (item.Type)
        {
            case RowType.Level2Header:
                backgroundColor = Windows.UI.Color.FromArgb(255, 255, 243, 205); // #FFF3CD Yellow
                isHeader = true;
                isBold = true;
                break;

            case RowType.Level3Header_A:
                backgroundColor = Windows.UI.Color.FromArgb(255, 212, 237, 218); // #D4EDDA Green
                isHeader = true;
                isBold = true;
                break;

            case RowType.Level3Header_B:
                backgroundColor = Windows.UI.Color.FromArgb(255, 204, 229, 255); // #CCE5FF Blue
                isHeader = true;
                isBold = true;
                break;

            case RowType.Level3Header_C:
                backgroundColor = Windows.UI.Color.FromArgb(255, 248, 215, 218); // #F8D7DA Red
                isHeader = true;
                isBold = true;
                break;

            case RowType.File:
                backgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255); // White
                break;

            case RowType.Blank:
                backgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255); // White
                break;
        }

        // Add the content text
        var contentText = new TextBlock
        {
            Text = item.Content ?? string.Empty,
            FontSize = 11,
            Margin = new Thickness(4, 2, 4, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
            TextWrapping = TextWrapping.Wrap
        };

        if (isBold)
        {
            contentText.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            contentText.FontSize = 13;
        }

        // Place content based on row type
        if (isHeader)
        {
            // Headers: create a colored background grid that spans columns 1-4
            var headerGrid = new Grid
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(backgroundColor)
            };
            headerGrid.Children.Add(contentText);

            Grid.SetColumn(headerGrid, 1);
            Grid.SetColumnSpan(headerGrid, 4);
            rowGrid.Children.Add(headerGrid);
        }
        else
        {
            // Regular files: white background, content in column 2
            rowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(backgroundColor);
            Grid.SetColumn(contentText, 2);
            rowGrid.Children.Add(contentText);
        }

        return rowGrid;
    }
}