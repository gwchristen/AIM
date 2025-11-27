using AIM.Models;
using AIM.Services;
using AIM.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            // DEBUG: Show what we received after page is fully loaded
            this.Loaded += async (s, args) =>
            {
                await Task.Delay(500); // Wait for everything to settle

                try
                {
                    var debugInfo = string.Join("\n", Services.PrintPaginationService.PaginationLog);

                    if (form.Pages?.Count > 0)
                    {
                        var firstPage = form.Pages[0];
                        debugInfo += $"\n\nFirst Page:\n" +
                                     $"- Level2Header: {firstPage.Level2Header}\n" +
                                     $"- Rows count: {firstPage.Rows?.Count ?? 0}";

                        if (firstPage.Rows?.Count > 0)
                        {
                            debugInfo += $"\n- First 5 rows:";
                            foreach (var row in firstPage.Rows.Take(5))
                            {
                                var content = row.Content ?? "(null)";
                                if (content.Length > 30) content = content.Substring(0, 30) + "...";
                                debugInfo += $"\n  [{row.Type}] {content}";
                            }
                        }
                    }

                    if (this.XamlRoot != null)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "DEBUG: Form Data",
                            Content = new ScrollViewer
                            {
                                Content = new TextBlock
                                {
                                    Text = debugInfo,
                                    TextWrapping = TextWrapping.Wrap,
                                    IsTextSelectionEnabled = true
                                },
                                MaxHeight = 400
                            },
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Debug dialog error: {ex.Message}");
                }
            };
        }

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
        double pageWidth = 816;
        double pageHeight = 1056;

        // REMOVE Width and Height constraints to see overflow
        var outerGrid = new Grid
        {
            // Width = pageWidth,      // COMMENTED OUT
            // Height = pageHeight,    // COMMENTED OUT
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
        };

        var pageGrid = new Grid
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            Margin = new Thickness(40),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        pageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });      // Row 0: Page Header
        pageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });      // Row 1: Level2 Header
        pageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });      // Row 2: Content - CHANGED FROM Star TO Auto
        pageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });      // Row 3: Footer

        // ========== ROW 0: PAGE HEADER with Initials ==========
        var headerBorder = new Border
        {
            Padding = new Thickness(12),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
            BorderThickness = new Thickness(0, 0, 0, 2)
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left side: Title and subtitle
        var headerStackPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };

        // Determine header color based on content (matching HeaderColorConverter logic)
        var headerColor = GetHeaderColor(page.PageHeader);

        var headerText = new TextBlock
        {
            Text = page.PageHeader ?? _viewModel?.FormData?.Header ?? string.Empty,
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(headerColor)
        };
        headerStackPanel.Children.Add(headerText);

        var subtitleText = new TextBlock
        {
            Text = "Inventory Summary",
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102))
        };
        headerStackPanel.Children.Add(subtitleText);

        Grid.SetColumn(headerStackPanel, 0);
        headerGrid.Children.Add(headerStackPanel);

        // Right side: Initials
        var initialsText = new TextBlock
        {
            Text = "Initials: __________",
            FontSize = 11,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetColumn(initialsText, 1);
        headerGrid.Children.Add(initialsText);

        headerBorder.Child = headerGrid;
        Grid.SetRow(headerBorder, 0);
        pageGrid.Children.Add(headerBorder);

        // ========== ROW 1: LEVEL 2 HEADER ==========
        if (!string.IsNullOrEmpty(page.Level2Header))
        {
            var level2Border = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 243, 205)),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
                BorderThickness = new Thickness(1, 1, 1, 0),
                Padding = new Thickness(8)
            };

            var level2Text = new TextBlock
            {
                Text = page.Level2Header + (page.IsContinuationPage ? " (Continued)" : ""),
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0))
            };
            level2Border.Child = level2Text;

            Grid.SetRow(level2Border, 1);
            pageGrid.Children.Add(level2Border);
        }

        // ========== ROW 2: CONTENT ROWS ==========
        var contentBorder = new Border
        {
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
            BorderThickness = new Thickness(1),
        };

        var contentPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (page.Rows != null)
        {
            foreach (var row in page.Rows)
            {
                var rowElement = CreatePrintRowVisual(row);
                contentPanel.Children.Add(rowElement);
            }
        }

        contentBorder.Child = contentPanel;
        Grid.SetRow(contentBorder, 2);
        pageGrid.Children.Add(contentBorder);

        // ========== ROW 3: FOOTER ==========
        var footerBorder = new Border
        {
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
            BorderThickness = new Thickness(1, 2, 1, 1),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 249, 249, 249)),
            Padding = new Thickness(10)
        };

        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var generatedText = new TextBlock
        {
            Text = "Generated by AIM Inventory Management",
            FontSize = 10,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102))
        };
        Grid.SetColumn(generatedText, 0);
        footerGrid.Children.Add(generatedText);

        var pageNumberText = new TextBlock
        {
            Text = $"Page {page.PageNumber} of {page.TotalPages}",
            FontSize = 10,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102))
        };
        Grid.SetColumn(pageNumberText, 1);
        footerGrid.Children.Add(pageNumberText);

        footerBorder.Child = footerGrid;
        Grid.SetRow(footerBorder, 3);
        pageGrid.Children.Add(footerBorder);

        outerGrid.Children.Add(pageGrid);

        // COMMENT OUT the Measure/Arrange to see natural content size
        // outerGrid.Measure(new Windows.Foundation. Size(pageWidth, pageHeight));
        // outerGrid. Arrange(new Windows.Foundation.Rect(0, 0, pageWidth, pageHeight));

        return outerGrid;
    }

    /// <summary>
    /// Gets the header color based on the header text (matches HeaderColorConverter logic). 
    /// </summary>
    private Windows.UI.Color GetHeaderColor(string? header)
    {
        if (string.IsNullOrEmpty(header))
            return Windows.UI.Color.FromArgb(255, 0, 0, 0); // Black default

        // Add your color logic here based on header content
        // Example: different colors for different regions/divisions
        var headerLower = header.ToLowerInvariant();

        if (headerLower.Contains("ohio"))
            return Windows.UI.Color.FromArgb(255, 0, 100, 0); // Dark Green
        else if (headerLower.Contains("i&m") || headerLower.Contains("i & m"))
            return Windows.UI.Color.FromArgb(255, 0, 0, 139); // Dark Blue
                                                              // Add more conditions as needed based on your HeaderColorConverter

        return Windows.UI.Color.FromArgb(255, 0, 0, 0); // Black default
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

        // In CreatePrintRowVisual method, change the contentText FontSize from 11 to 12:
        var contentText = new TextBlock
        {
            Text = item.Content ?? string.Empty,
            FontSize = 12,  // Changed from 11 to 12
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
    // Add this method to PrintableFormPage. xaml.cs
    private async void ShowPaginationDebug()
    {
        var sb = new System.Text.StringBuilder();

        // Show the pagination log
        sb.AppendLine("=== PAGINATION LOG ===");
        foreach (var line in Services.PrintPaginationService.PaginationLog)
        {
            sb.AppendLine(line);
        }

        // Write to a file we can access
        try
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "pagination_log.txt");
            System.IO.File.WriteAllText(logPath, sb.ToString());
            sb.AppendLine($"\n\nLog saved to: {logPath}");
        }
        catch { }

        var dialog = new ContentDialog
        {
            Title = "Pagination Debug",
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = sb.ToString(),
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 10,
                    TextWrapping = TextWrapping.NoWrap,
                    IsTextSelectionEnabled = true
                },
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 500,
                MaxWidth = 800
            },
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}