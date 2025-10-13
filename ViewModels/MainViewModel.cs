using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace AIM.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private DirectoryItem selectedRootDirectory;

    public ObservableCollection<DirectoryItem> DirectoryItems { get; } = new();

    public MainViewModel(IFileService fileService, ILoggingService loggingService)
    {
        _fileService = fileService;
        _loggingService = loggingService;
        LoadInitialDirectories();
    }

    private void LoadInitialDirectories()
    {
        // Optionally load drives or leave empty until root is selected
    }

    public async Task LoadRootDirectoryAsync(string path)
    {
        var root = new DirectoryItem { Name = Path.GetFileName(path), FullPath = path };
        SelectedRootDirectory = root;
        DirectoryItems.Clear();
        DirectoryItems.Add(root);
        await _fileService.LoadDirectoryAsync(root);
        await _loggingService.LogAsync("Loaded root directory", path);
    }

    public void HandleFileDrop(DataPackageView dataView)
    {
        // Implement drop logic
        _loggingService.LogAsync("File dropped for addition");
    }

    [RelayCommand]
    private void RenameItem() { }

    [RelayCommand]
    private void DeleteToArchive() { }

    [RelayCommand]
    private void ShipItems() { }
}