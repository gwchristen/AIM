#pragma warning disable MVVMTK0045
using AIM.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace AIM.ViewModels;

public class AppSettings
{
    public string ArchivePath { get; set; } = @"C:\Temp\AIM\Archive";
    public string ShippedDirectory { get; set; } = @"C:\Temp\AIM\Shipped";
    public string Password { get; set; } = string.Empty;
    public string DefaultRootDirectory { get; set; } = @"C:\Temp\AIM";
    public string FileScansDirectory { get; set; } = @"C:\Temp\AIM\FileScans";
    public string InventoryArchiveDirectory { get; set; } = @"C:\Temp\AIM\InventoryArchive";
}

public partial class MainViewModel : ObservableObject
{
    private readonly string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AIM", "settings.json");

    private string _selectedRoot = @"C:\Temp\AIM";

    [ObservableProperty]
    private ObservableCollection<DirectoryItem> directoryItems = new();

    [ObservableProperty]
    private DirectoryItem selectedDirectory;

    [ObservableProperty]
    private DirectoryItem selectedLevel1;

    [ObservableProperty]
    private DirectoryItem selectedLevel2;

    [ObservableProperty]
    private DirectoryItem selectedLevel3;

    [ObservableProperty]
    private DirectoryItem selectedLevel4;

    [ObservableProperty]
    private string rootName = string.Empty;

    private string _archivePath = @"C:\Temp\AIM\Archive";

    private string _shippedDirectory = @"C:\Temp\AIM\Shipped";

    private string _password = string.Empty;

    private string _defaultRootDirectory = @"C:\Temp\AIM";

    private string _fileScansDirectory = @"C:\Temp\AIM\FileScans";

    private string _inventoryArchiveDirectory = @"C:\Temp\AIM\InventoryArchive";

    [ObservableProperty]
    private ObservableCollection<AIM.Models.FileItem> selectedScanFiles = new();

    public string ArchivePath
    {
        get => _archivePath;
        set
        {
            if (SetProperty(ref _archivePath, value))
            {
                SaveSettings();
            }
        }
    }

    public string ShippedDirectory
    {
        get => _shippedDirectory;
        set
        {
            if (SetProperty(ref _shippedDirectory, value))
            {
                SaveSettings();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                SaveSettings();
            }
        }
    }

    public string DefaultRootDirectory
    {
        get => _defaultRootDirectory;
        set
        {
            if (SetProperty(ref _defaultRootDirectory, value))
            {
                SaveSettings();
            }
        }
    }

    public string FileScansDirectory
    {
        get => _fileScansDirectory;
        set
        {
            if (SetProperty(ref _fileScansDirectory, value))
            {
                SaveSettings();
            }
        }
    }

    public string InventoryArchiveDirectory
    {
        get => _inventoryArchiveDirectory;
        set
        {
            if (SetProperty(ref _inventoryArchiveDirectory, value))
            {
                SaveSettings();
            }
        }
    }

    public ObservableCollection<DirectoryItem> Level1 { get; } = new();
    public ObservableCollection<DirectoryItem> Level2 { get; } = new();
    public ObservableCollection<DirectoryItem> Level3 { get; } = new();
    public ObservableCollection<DirectoryItem> Level4 { get; } = new();

    private DispatcherTimer? _refreshTimer;

    public string SelectedRoot
    {
        get => _selectedRoot;
        set
        {
            if (SetProperty(ref _selectedRoot, value))
            {
                OnSelectedRootChanged();
            }
        }
    }

    public MainViewModel()
    {
        LoadSettings();
        OnSelectedRootChanged();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refreshTimer.Tick += (s, e) => RefreshTree();
        _refreshTimer.Start();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFilePath))
            {
                var json = File.ReadAllText(settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    _archivePath = settings.ArchivePath ?? _archivePath;
                    _shippedDirectory = settings.ShippedDirectory ?? _shippedDirectory;
                    _password = settings.Password ?? _password;
                    _defaultRootDirectory = settings.DefaultRootDirectory ?? _defaultRootDirectory;
                    _fileScansDirectory = settings.FileScansDirectory ?? _fileScansDirectory;
                    _inventoryArchiveDirectory = settings.InventoryArchiveDirectory ?? _inventoryArchiveDirectory;
                }
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new AppSettings
            {
                ArchivePath = _archivePath,
                ShippedDirectory = _shippedDirectory,
                Password = _password,
                DefaultRootDirectory = _defaultRootDirectory,
                FileScansDirectory = _fileScansDirectory,
                InventoryArchiveDirectory = _inventoryArchiveDirectory
            };
            var json = JsonSerializer.Serialize(settings);
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath));
            File.WriteAllText(settingsFilePath, json);
        }
        catch { }
    }

    private void OnSelectedRootChanged()
    {
        // Save selected paths before clearing
        var sel1Path = SelectedLevel1?.FullPath;
        var sel2Path = SelectedLevel2?.FullPath;
        var sel3Path = SelectedLevel3?.FullPath;
        var sel4Path = SelectedLevel4?.FullPath;

        DirectoryItems.Clear();
        Level1.Clear();
        Level2.Clear();
        Level3.Clear();
        Level4.Clear();

        if (Directory.Exists(SelectedRoot))
        {
            var root = new DirectoryItem
            {
                Name = Path.GetFileName(SelectedRoot),
                FullPath = SelectedRoot
            };
            PopulateSubDirectories(root);
            DirectoryItems.Add(root);
            RootName = root.Name;
            foreach (var sub in root.SubDirectories.Where(s => HasContents(s)))
            {
                Level1.Add(sub);
            }
        }
        else
        {
            // Create the directory and some test subdirectories
            Directory.CreateDirectory(SelectedRoot);
            Directory.CreateDirectory(Path.Combine(SelectedRoot, "SubDir1"));
            Directory.CreateDirectory(Path.Combine(SelectedRoot, "SubDir2"));
            File.WriteAllText(Path.Combine(SelectedRoot, "test.txt"), "Test content");
            File.WriteAllText(Path.Combine(SelectedRoot, "SubDir1", "file1.txt"), "Content 1");
            File.WriteAllText(Path.Combine(SelectedRoot, "SubDir2", "file2.csv"), "Col1,Col2\nVal1,Val2");

            var root = new DirectoryItem
            {
                Name = Path.GetFileName(SelectedRoot),
                FullPath = SelectedRoot
            };
            PopulateSubDirectories(root);
            DirectoryItems.Add(root);
            RootName = root.Name;
            foreach (var sub in root.SubDirectories.Where(s => HasContents(s)))
            {
                Level1.Add(sub);
            }
        }

        // Restore selections
        SelectedLevel1 = Level1.FirstOrDefault(d => d.FullPath == sel1Path);
        if (SelectedLevel1 != null)
        {
            PopulateSubDirectories(SelectedLevel1);
            SelectedLevel2 = SelectedLevel1.SubDirectories.FirstOrDefault(d => d.FullPath == sel2Path);
            if (SelectedLevel2 != null)
            {
                PopulateSubDirectories(SelectedLevel2);
                SelectedLevel3 = SelectedLevel2.SubDirectories.FirstOrDefault(d => d.FullPath == sel3Path);
                if (SelectedLevel3 != null)
                {
                    PopulateSubDirectories(SelectedLevel3);
                    SelectedLevel4 = SelectedLevel3.SubDirectories.FirstOrDefault(d => d.FullPath == sel4Path);
                }
            }
        }

        UpdateSelectedDirectory();
    }

    private bool HasContents(DirectoryItem item)
    {
        try
        {
            return item.SubDirectories.Any() || Directory.GetFiles(item.FullPath).Any();
        }
        catch
        {
            return false;
        }
    }

    private void PopulateSubDirectories(DirectoryItem item)
    {
        try
        {
            var subDirs = Directory.GetDirectories(item.FullPath)
                .Select(d => new DirectoryItem
                {
                    Name = Path.GetFileName(d),
                    FullPath = d
                })
                .ToList();

            foreach (var sub in subDirs)
            {
                PopulateSubDirectories(sub);
                item.SubDirectories.Add(sub);
            }
        }
        catch
        {
            // Ignore access denied etc.
        }
    }

    [RelayCommand]
    private void RefreshTree()
    {
        OnSelectedRootChanged();
    }

    public void UpdateSelectedDirectory()
    {
        SelectedDirectory = SelectedLevel4 ?? SelectedLevel3 ?? SelectedLevel2 ?? SelectedLevel1 ?? DirectoryItems[0];
    }
}