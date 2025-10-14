using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Threading.Tasks;

namespace AIM.ViewModels;

public class PreviewViewModel : ObservableObject
{
    private readonly IFileService _fileService;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private string fileName = string.Empty;

    public PreviewViewModel()
    {
        _fileService = Ioc.Default.GetService<IFileService>();
    }

    public async Task LoadFileContent(FileItem file)
    {
        if (file.Type == FileType.Text || file.Type == FileType.Csv || file.Type == FileType.Log)
        {
            Content = await _fileService.ReadFilePreviewAsync(file.FullPath);
            FileName = file.Name;
        }
        else
        {
            Content = "Preview not supported for this file type.";
            FileName = file.Name;
        }
    }

    public async Task SaveFileContent()
    {
        if (!string.IsNullOrEmpty(FileName))
        {
            // Placeholder: Implement save logic with full path
        }
    }
}