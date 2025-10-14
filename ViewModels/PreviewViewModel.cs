using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly IFileService _fileService;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private string fileName = string.Empty;

    public PreviewViewModel()
    {
        _fileService = Ioc.Default.GetService<IFileService>()!;
    }

    public async Task LoadFileContent(FileItem file)
    {
        //Debug.WriteLine($"LoadFileContent: {file.FullPath}, Type: {file.Type}"); // Debug output
        Content = $"File: {file.FullPath}, Type: {file.Type}"; // Temporary display
        FileName = file.Name;
        if (file.Type == FileType.Text || file.Type == FileType.Csv || file.Type == FileType.Log)
        {
            try
            {
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

    public async Task SaveFileContent()
    {
        await Task.CompletedTask; // Fix CS1998
        if (!string.IsNullOrEmpty(FileName))
        {
            // Placeholder: Implement save logic with full path
        }
    }
}