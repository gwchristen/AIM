using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private string fileName = string.Empty;

    // The constructor now correctly receives the service via DI.
    public PreviewViewModel(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task LoadFileContent(FileItem file)
    {
        _currentFilePath = file.FullPath;
        if (file.Type == FileType.Text || file.Type == FileType.Csv || file.Type == FileType.Log)
        {
            try
            {
                // This call will now succeed.
                Content = await _fileService.ReadFilePreviewAsync(file.FullPath);
                FileName = file.Name;
            }
            catch (Exception ex)
            {
                Content = $"Error loading file: {ex.Message}";
            }
        }
        else
        {
            Content = "Preview not supported for this file type.";
            FileName = file.Name;
        }
    }

    [RelayCommand]
    public async Task SaveFileContent()
    {
        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            try
            {
                // This call will now succeed.
                await _fileService.WriteFileAsync(_currentFilePath, Content);
            }
            catch (Exception ex)
            {
                // Optionally show error (e.g., via dialog or status)
            }
        }
    }
}