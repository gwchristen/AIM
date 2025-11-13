using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    partial void OnTextContentChanged(string? value) => IsDirty = (value != _originalTextContent);
    partial void OnIsDirtyChanged(bool value) => SaveContentCommand.NotifyCanExecuteChanged();

    public async Task OnNavigatedTo(object parameter)
    {
        IsLoading = true;
        IsDirty = false;
        TextContent = string.Empty;
        CsvData = null;
        IsTextVisible = false;
        IsCsvVisible = false;

        if (parameter is FileItem fileItem)
        {
            FileItem = fileItem;
            if (fileItem.Type == FileType.Csv)
            {
                await LoadCsvAsync(fileItem.FullPath);
                IsCsvVisible = true;
            }
            else
            {
                await LoadTextAsync(fileItem.FullPath);
                IsTextVisible = true;
            }
        }
        IsLoading = false;
    }

    private async Task LoadTextAsync(string filePath)
    {
        try
        {
            _originalTextContent = await File.ReadAllTextAsync(filePath);
            TextContent = _originalTextContent;
        }
        catch (Exception ex)
        {
            TextContent = $"Error reading file: {ex.Message}";
            _originalTextContent = TextContent;
        }
        IsDirty = false;
    }

    private async Task LoadCsvAsync(string filePath) { var dataTable = new DataTable(); try { var lines = await File.ReadAllLinesAsync(filePath); if (lines.Length == 0) return; var headers = lines[0].Split(','); foreach (var header in headers) { dataTable.Columns.Add(header.Trim()); } foreach (var line in lines.Skip(1)) { var fields = line.Split(','); dataTable.Rows.Add(fields); } CsvData = dataTable; } catch (Exception ex) { dataTable = new DataTable(); dataTable.Columns.Add("Error"); dataTable.Rows.Add($"Could not parse CSV file: {ex.Message}"); CsvData = dataTable; } }
    [RelayCommand(CanExecute = nameof(IsDirty))] private async Task SaveContent() { if (FileItem == null || !IsDirty) return; IsLoading = true; try { if (IsTextVisible && TextContent != null) { await File.WriteAllTextAsync(FileItem.FullPath, TextContent); _originalTextContent = TextContent; IsDirty = false; _infoBarService.Show("Success", "File saved successfully.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, 3000); } } catch (Exception ex) { _infoBarService.Show("Error", $"Could not save file: {ex.Message}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, 5000); } finally { IsLoading = false; } }
    [RelayCommand] private void CopyContent() { if (FileItem == null) return; var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage(); string contentToCopy = string.Empty; if (IsCsvVisible && CsvData != null) { var stringBuilder = new StringBuilder(); var columnNames = CsvData.Columns.Cast<DataColumn>().Select(column => column.ColumnName); stringBuilder.AppendLine(string.Join(",", columnNames)); foreach (DataRow row in CsvData.Rows) { var fields = row.ItemArray.Select(field => field.ToString()); stringBuilder.AppendLine(string.Join(",", fields)); } contentToCopy = stringBuilder.ToString(); } else if (IsTextVisible && TextContent != null) { contentToCopy = TextContent; } if (!string.IsNullOrEmpty(contentToCopy)) { dataPackage.SetText(contentToCopy); Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage); } }
}