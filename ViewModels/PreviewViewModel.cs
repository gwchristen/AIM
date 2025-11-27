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
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _fileIcon = "\uE8A5";

    [ObservableProperty]
    private string _fileSizeText = string.Empty;

    [ObservableProperty]
    private string _fileModifiedText = string.Empty;

    [ObservableProperty]
    private int _lineCount;

    [ObservableProperty]
    private int _characterCount;
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
            LineCount = 0;
            CharacterCount = 0;
        }
        else
        {
            LineCount = TextContent.Split('\n').Length;
            CharacterCount = TextContent.Length;
        }
    }
    #endregion

    public async Task OnNavigatedTo(object parameter)
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

        if (parameter is FileItem fileItem)
        {
            FileItem = fileItem;
            await LoadFileMetadata(fileItem.FullPath);

            if (fileItem.Type == FileType.Csv)
            {
                FileIcon = "\uE9D9"; // Table icon
                await LoadCsvAsync(fileItem.FullPath);
                if (!HasError) IsCsvVisible = true;
            }
            else
            {
                FileIcon = "\uE8A5"; // Document icon
                await LoadTextAsync(fileItem.FullPath);
                if (!HasError) IsTextVisible = true;
            }
        }
        IsLoading = false;
    }

    private async Task LoadFileMetadata(string filePath)
    {
        try
        {
            await Task.Run(() =>
            {
                var fileInfo = new FileInfo(filePath);
                FileSizeText = FormatFileSize(fileInfo.Length);
                FileModifiedText = $"Modified {fileInfo.LastWriteTime:g}";
                IsReadOnly = fileInfo.IsReadOnly;
            });
        }
        catch
        {
            FileSizeText = "Unknown size";
            FileModifiedText = "";
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
            ErrorMessage = "Access denied.  You don't have permission to read this file. ";
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
                _infoBarService.Show("Success", "File saved successfully.", InfoBarSeverity.Success);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _infoBarService.Show("Save Failed", "Access denied.  The file may be read-only or in use.", InfoBarSeverity.Error);
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
        if (FileItem == null) return;

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
            _infoBarService.Show("Copied", "Content copied to clipboard.", InfoBarSeverity.Success);
        }
    }
}