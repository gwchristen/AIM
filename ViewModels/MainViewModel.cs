#pragma warning disable MVVMTK0045
using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace AIM.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly ILoggingService _loggingService;

    private string selectedRoot = string.Empty;
    private string archivePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Archive");
    private string defaultRootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private string shippedDirectory = string.Empty;
    private string fileScansDirectory = string.Empty;
    private string inventoryArchiveDirectory = string.Empty;
    private string password = string.Empty;

    private string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AIM", "settings.json");

    [ObservableProperty]
    private DirectoryItem selectedRootDirectory;

    public string SelectedRoot
    {
        get => selectedRoot;
        set
        {
            if (SetProperty(ref selectedRoot, value))
            {
                OnSelectedRootChanged(value);
            }
        }
    }

    public string ArchivePath
    {
        get => archivePath;
        set
        {
            if (SetProperty(ref archivePath, value))
            {
                SaveSettings();
            }
        }
    }

    public string DefaultRootDirectory
    {
        get => defaultRootDirectory;
        set
        {
            if (SetProperty(ref defaultRootDirectory, value))
            {
                OnDefaultRootDirectoryChanged(value);
                SaveSettings();
            }
        }
    }

    public string ShippedDirectory
    {
        get => shippedDirectory;
        set
        {
            if (SetProperty(ref shippedDirectory, value))
            {
                SaveSettings();
            }
        }
    }

    public string FileScansDirectory
    {
        get => fileScansDirectory;
        set
        {
            if (SetProperty(ref fileScansDirectory, value))
            {
                SaveSettings();
            }
        }
    }

    public string InventoryArchiveDirectory
    {
        get => inventoryArchiveDirectory;
        set
        {
            if (SetProperty(ref inventoryArchiveDirectory, value))
            {
                SaveSettings();
            }
        }
    }

    public string Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                SaveSettings();
            }
        }
    }

    public ObservableCollection<DirectoryItem> DirectoryItems { get; } = new();

    public List<RootOption> RootOptions { get; } = new()
    {
        new RootOption { Name = "Documents", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
        new RootOption { Name = "Desktop", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
        new RootOption { Name = "Downloads", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads") },
        new RootOption { Name = "Pictures", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) }
    };

    public MainViewModel(IFileService fileService, ILoggingService loggingService)
    {
        _fileService = fileService;
        _loggingService = loggingService;
        LoadSettings();
        SelectedRoot = !string.IsNullOrEmpty(DefaultRootDirectory) ? DefaultRootDirectory : RootOptions.FirstOrDefault()?.Path ?? string.Empty;
        if (!string.IsNullOrEmpty(SelectedRoot))
        {
            _ = LoadRootDirectoryAsync(SelectedRoot);
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                ArchivePath = data.ArchivePath ?? ArchivePath;
                DefaultRootDirectory = data.DefaultRootDirectory ?? DefaultRootDirectory;
                ShippedDirectory = data.ShippedDirectory ?? ShippedDirectory;
                FileScansDirectory = data.FileScansDirectory ?? FileScansDirectory;
                InventoryArchiveDirectory = data.InventoryArchiveDirectory ?? InventoryArchiveDirectory;
                Password = data.Password ?? Password;
            }
        }
        catch
        {
            // Use defaults if loading fails
        }
    }

    private void SaveSettings()
    {
        try
        {
            var data = new SettingsData
            {
                ArchivePath = ArchivePath,
                DefaultRootDirectory = DefaultRootDirectory,
                ShippedDirectory = ShippedDirectory,
                FileScansDirectory = FileScansDirectory,
                InventoryArchiveDirectory = InventoryArchiveDirectory,
                Password = Password
            };
            var dir = Path.GetDirectoryName(settingsFile);
            Directory.CreateDirectory(dir);
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(data));
        }
        catch
        {
            // Ignore if saving fails
        }
    }

    public async Task LoadRootDirectoryAsync(string path)
    {
        try
        {
            var root = new DirectoryItem { Name = Path.GetFileName(path), FullPath = path };
            SelectedRootDirectory = root;
            DirectoryItems.Clear();
            DirectoryItems.Add(root);
            await LoadDirectoryAsync(root, 1);
            await _loggingService.LogAsync("Loaded root directory", path);
        }
        catch (UnauthorizedAccessException)
        {
            // Handle access denied for root
        }
    }

    public async Task LoadDirectoryAsync(DirectoryItem item, int depth = 0)
    {
        if (item == null || depth > 5) return;
        try
        {
            var subDirs = Directory.GetDirectories(item.FullPath)
                .Select(d => new DirectoryItem { Name = Path.GetFileName(d), FullPath = d })
                .ToList();
            item.SubDirectories.Clear();
            foreach (var sub in subDirs)
            {
                item.SubDirectories.Add(sub);
            }
            // Recursively load deeper
            foreach (var sub in item.SubDirectories)
            {
                await LoadDirectoryAsync(sub, depth + 1);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Handle access denied
        }
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

    private void OnSelectedRootChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadRootDirectoryAsync(value);
        }
    }

    private void OnDefaultRootDirectoryChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            SelectedRoot = value;
        }
    }
}

public class RootOption
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public class SettingsData
{
    public string ArchivePath { get; set; }
    public string DefaultRootDirectory { get; set; }
    public string ShippedDirectory { get; set; }
    public string FileScansDirectory { get; set; }
    public string InventoryArchiveDirectory { get; set; }
    public string Password { get; set; }
}