using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AIM.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly IInfoBarService _infoBarService;
    private string? _originalTextContent;

    public PreviewViewModel(IInfoBarService infoBarService)
    {
        _infoBarService = infoBarService;
    }

    #region Observable Properties
    [ObservableProperty]
    private FileItem? _fileItem;

    [ObservableProperty]
    private string _fileName = "No file";

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string? _textContent;

    [ObservableProperty]
    private DataTable? _csvData;

    [ObservableProperty]
    private bool _isTextVisible;

    [ObservableProperty]
    private bool _isCsvVisible;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isWordWrapEnabled = false;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isReadOnly;

    [ObservableProperty]
    private bool _isExternalFile;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _showEmptyState = true;

    [ObservableProperty]
    private bool _hasFileInfo;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _fileIcon = "\uE8A5";

    [ObservableProperty]
    private string _fileSizeText = string.Empty;

    [ObservableProperty]
    private string _fileModifiedText = string.Empty;

    [ObservableProperty]
    private int _lineCount = 1;

    [ObservableProperty]
    private int _characterCount;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private int _selectionLength;

    [ObservableProperty]
    private int _currentLine = 1;

    [ObservableProperty]
    private int _currentColumn = 1;
    #endregion

    #region Property Changed Handlers
    partial void OnTextContentChanged(string? value)
    {
        IsDirty = value != _originalTextContent;
        UpdateTextStats();
    }

    partial void OnIsDirtyChanged(bool value) => SaveContentCommand.NotifyCanExecuteChanged();

    private void UpdateTextStats()
    {
        if (string.IsNullOrEmpty(TextContent))
        {
            LineCount = 1;
            CharacterCount = 0;
        }
        else
        {
            CharacterCount = TextContent.Length;
            LineCount = GetLineCount(TextContent);
        }
    }

    private int GetLineCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        int lines = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                lines++;
                if (i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
            }
            else if (text[i] == '\n')
            {
                lines++;
            }
        }
        return lines;
    }

    public void UpdateCursorPosition(int selectionStart, int selectionLength)
    {
        SelectionLength = selectionLength;
        HasSelection = selectionLength > 0;

        if (string.IsNullOrEmpty(TextContent) || selectionStart < 0)
        {
            CurrentLine = 1;
            CurrentColumn = 1;
            return;
        }

        selectionStart = Math.Min(selectionStart, TextContent.Length);

        int line = 1;
        int lastLineStart = 0;

        for (int i = 0; i < selectionStart; i++)
        {
            if (TextContent[i] == '\r')
            {
                line++;
                if (i + 1 < TextContent.Length && TextContent[i + 1] == '\n')
                    i++;
                lastLineStart = i + 1;
            }
            else if (TextContent[i] == '\n')
            {
                line++;
                lastLineStart = i + 1;
            }
        }

        CurrentLine = line;
        CurrentColumn = selectionStart - lastLineStart + 1;
    }

    /// <summary>
    /// Gets the start index and length for a given line number
    /// </summary>
    public (int startIndex, int length) GetLineRange(int lineNumber)
    {
        if (string.IsNullOrEmpty(TextContent) || lineNumber < 1)
            return (0, 0);

        int currentLine = 1;
        int lineStart = 0;

        for (int i = 0; i < TextContent.Length; i++)
        {
            if (currentLine == lineNumber)
            {
                int lineEnd = i;
                while (lineEnd < TextContent.Length &&
                       TextContent[lineEnd] != '\r' &&
                       TextContent[lineEnd] != '\n')
                {
                    lineEnd++;
                }
                return (i, lineEnd - i);
            }

            if (TextContent[i] == '\r')
            {
                currentLine++;
                if (i + 1 < TextContent.Length && TextContent[i + 1] == '\n')
                    i++;
                lineStart = i + 1;
            }
            else if (TextContent[i] == '\n')
            {
                currentLine++;
                lineStart = i + 1;
            }
        }

        if (currentLine == lineNumber)
        {
            return (lineStart, TextContent.Length - lineStart);
        }

        return (0, 0);
    }

    /// <summary>
    /// Gets the character index for a given line number (start of line)
    /// </summary>
    public int GetCharIndexForLine(int lineNumber)
    {
        return GetLineRange(lineNumber).startIndex;
    }
    #endregion

    public async Task OnNavigatedTo(object? parameter)
    {
        if (parameter is FileItem fileItem)
        {
            await LoadFile(fileItem.FullPath, fileItem.Type == FileType.Csv);
            IsExternalFile = false;
        }
        else
        {
            ResetState();
            ShowEmptyState = true;
        }
    }

    private void ResetState()
    {
        IsLoading = false;
        IsDirty = false;
        HasError = false;
        ErrorMessage = string.Empty;
        TextContent = string.Empty;
        CsvData = null;
        IsTextVisible = false;
        IsCsvVisible = false;
        IsReadOnly = false;
        HasFileInfo = false;
        FileName = "No file";
        FilePath = "";
        FileItem = null;
        LineCount = 1;
        CharacterCount = 0;
        CurrentLine = 1;
        CurrentColumn = 1;
    }

    private async Task LoadFile(string filePath, bool isCsv = false)
    {
        IsLoading = true;
        IsDirty = false;
        HasError = false;
        ErrorMessage = string.Empty;
        TextContent = string.Empty;
        CsvData = null;
        IsTextVisible = false;
        IsCsvVisible = false;
        IsReadOnly = false;
        ShowEmptyState = false;

        FileName = Path.GetFileName(filePath);
        FilePath = filePath;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        FileItem = new FileItem
        {
            Name = FileName,
            FullPath = filePath,
            Type = ext == ".csv" ? FileType.Csv : FileType.Text
        };

        await LoadFileMetadata(filePath);

        if (isCsv || ext == ".csv")
        {
            FileIcon = "\uE9D9";
            await LoadCsvAsync(filePath);
            if (!HasError) IsCsvVisible = true;
        }
        else
        {
            FileIcon = "\uE8A5";
            await LoadTextAsync(filePath);
            if (!HasError) IsTextVisible = true;
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ". txt", ".csv", ".log", ".json", ".xml", ".md", ".ini", ".cfg", "*" }
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            var ext = Path.GetExtension(file.Path).ToLowerInvariant();
            await LoadFile(file.Path, ext == ". csv");
            IsExternalFile = true;
        }
    }

    private async Task LoadFileMetadata(string filePath)
    {
        try
        {
            // Run file operations on background thread, capture results
            var (size, modified, readOnly) = await Task.Run(() =>
            {
                var fileInfo = new FileInfo(filePath);
                return (
                    FormatFileSize(fileInfo.Length),
                    $"Modified {fileInfo.LastWriteTime:g}",
                    fileInfo.IsReadOnly
                );
            });

            // Update UI-bound properties on UI thread (after await, we're back on UI thread)
            FileSizeText = size;
            FileModifiedText = modified;
            IsReadOnly = readOnly;
            HasFileInfo = true;
        }
        catch
        {
            FileSizeText = "Unknown size";
            FileModifiedText = "";
            HasFileInfo = false;
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private async Task LoadTextAsync(string filePath)
    {
        try
        {
            _originalTextContent = await File.ReadAllTextAsync(filePath);
            TextContent = _originalTextContent;
            IsDirty = false;
        }
        catch (UnauthorizedAccessException)
        {
            HasError = true;
            ErrorMessage = "Access denied. You don't have permission to read this file.";
        }
        catch (FileNotFoundException)
        {
            HasError = true;
            ErrorMessage = "File not found.  It may have been moved or deleted.";
        }
        catch (IOException ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to read file: {ex.Message}";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
    }

    private async Task LoadCsvAsync(string filePath)
    {
        var dataTable = new DataTable();
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length == 0)
            {
                HasError = true;
                ErrorMessage = "The CSV file is empty.";
                return;
            }

            var headers = lines[0].Split(',');
            foreach (var header in headers)
            {
                dataTable.Columns.Add(header.Trim().Trim('"'));
            }

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var values = line.Split(',');
                var row = dataTable.NewRow();
                for (int i = 0; i < Math.Min(values.Length, dataTable.Columns.Count); i++)
                {
                    row[i] = values[i].Trim().Trim('"');
                }
                dataTable.Rows.Add(row);
            }
            CsvData = dataTable;
        }
        catch (UnauthorizedAccessException)
        {
            HasError = true;
            ErrorMessage = "Access denied. You don't have permission to read this file.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to parse CSV: {ex.Message}";
        }
    }

    public void DiscardChanges()
    {
        TextContent = _originalTextContent;
        IsDirty = false;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveContent()
    {
        if (FileItem == null || !IsDirty || IsReadOnly) return;

        IsLoading = true;
        try
        {
            if (IsTextVisible && TextContent != null)
            {
                await File.WriteAllTextAsync(FileItem.FullPath, TextContent);
                _originalTextContent = TextContent;
                IsDirty = false;

                // Refresh file metadata (now thread-safe)
                await LoadFileMetadata(FileItem.FullPath);

                _infoBarService.Show("Success", "File saved successfully.", InfoBarSeverity.Success, 2000);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _infoBarService.Show("Save Failed", "Access denied. The file may be read-only or in use.", InfoBarSeverity.Error);
        }
        catch (IOException ex)
        {
            _infoBarService.Show("Save Failed", $"Unable to save file: {ex.Message}", InfoBarSeverity.Error);
        }
        catch (Exception ex)
        {
            _infoBarService.Show("Save Failed", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave() => IsDirty && !IsReadOnly;

    [RelayCommand]
    private void CopyContent()
    {
        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        string contentToCopy = string.Empty;

        if (IsTextVisible && TextContent != null)
        {
            contentToCopy = TextContent;
        }
        else if (IsCsvVisible && CsvData != null)
        {
            var sb = new StringBuilder();
            var columnNames = CsvData.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
            sb.AppendLine(string.Join("\t", columnNames));
            foreach (DataRow row in CsvData.Rows)
            {
                sb.AppendLine(string.Join("\t", row.ItemArray));
            }
            contentToCopy = sb.ToString();
        }

        if (!string.IsNullOrEmpty(contentToCopy))
        {
            dataPackage.SetText(contentToCopy);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            _infoBarService.Show("Copied", "Content copied to clipboard.", InfoBarSeverity.Success, 2000);
        }
    }
}