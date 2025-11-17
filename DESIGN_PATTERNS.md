# Design Patterns in AIM

This document provides detailed explanations and examples of the design patterns used in the AIM (Asset Inventory Management) application.

## Table of Contents

- [MVVM Pattern](#mvvm-pattern)
- [Dependency Injection](#dependency-injection)
- [Service Layer Pattern](#service-layer-pattern)
- [Repository Pattern](#repository-pattern)
- [Command Pattern](#command-pattern)
- [Observer Pattern](#observer-pattern)
- [Messaging Pattern](#messaging-pattern)
- [Factory Pattern](#factory-pattern)
- [Strategy Pattern](#strategy-pattern)
- [Anti-Patterns to Avoid](#anti-patterns-to-avoid)

---

## MVVM Pattern

**Model-View-ViewModel** is the foundational architectural pattern in AIM, providing clean separation of concerns between UI and business logic.

### Components

#### Model

**Purpose**: Represents data and business entities without any UI logic.

**Example** (`/Models/FileItem.cs`):
```csharp
namespace AIM.Models;

/// <summary>
/// Represents a file in the file system.
/// </summary>
public class FileItem
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the full file path.
    /// </summary>
    public string FullPath { get; set; }
    
    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTime LastModified { get; set; }
    
    /// <summary>
    /// Gets or sets the file extension.
    /// </summary>
    public string Extension { get; set; }
}
```

**Characteristics**:
- Plain C# objects (POCOs)
- No dependencies on UI frameworks
- No business logic (only data)
- Can be easily serialized/deserialized

---

#### View

**Purpose**: Defines the visual structure and appearance using XAML.

**Example** (`/Views/MyPage.xaml`):
```xml
<Page
    x:Class="AIM.Views.MyPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:AIM.ViewModels">
    
    <Grid Padding="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <TextBlock 
            Grid.Row="0"
            Text="{x:Bind ViewModel.Title, Mode=OneWay}"
            Style="{StaticResource TitleTextBlockStyle}"
            Margin="0,0,0,20"/>
        
        <!-- Content List -->
        <ListView 
            Grid.Row="1"
            ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
            SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="vm:ItemViewModel">
                    <TextBlock Text="{x:Bind Name, Mode=OneWay}"/>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        
        <!-- Action Buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="10" Margin="0,20,0,0">
            <Button 
                Content="Load Data" 
                Command="{x:Bind ViewModel.LoadDataCommand}"/>
            <Button 
                Content="Save" 
                Command="{x:Bind ViewModel.SaveCommand}"
                IsEnabled="{x:Bind ViewModel.HasChanges, Mode=OneWay}"/>
        </StackPanel>
    </Grid>
</Page>
```

**Code-Behind** (`/Views/MyPage.xaml.cs`):
```csharp
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class MyPage : Page
{
    /// <summary>
    /// Gets the ViewModel for this page.
    /// </summary>
    public MyViewModel ViewModel { get; }
    
    public MyPage()
    {
        this.InitializeComponent();
        
        // Resolve ViewModel from DI container
        ViewModel = Ioc.Default.GetRequiredService<MyViewModel>();
        
        // Set DataContext for runtime binding (if needed)
        DataContext = ViewModel;
    }
}
```

**Characteristics**:
- XAML defines UI structure
- Code-behind is minimal (only initialization)
- No business logic in View
- Data binding connects View to ViewModel

---

#### ViewModel

**Purpose**: Contains presentation logic, state management, and exposes data/commands to the View.

**Example** (`/ViewModels/MyViewModel.cs`):
```csharp
using AIM.Models;
using AIM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AIM.ViewModels;

/// <summary>
/// ViewModel for MyPage.
/// </summary>
public partial class MyViewModel : ObservableObject
{
    #region Services
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly AuditLoggingService _auditService;
    #endregion
    
    #region Observable Properties
    
    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    [ObservableProperty]
    private string _title = "My Page";
    
    /// <summary>
    /// Gets or sets whether data is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;
    
    /// <summary>
    /// Gets or sets the selected item.
    /// </summary>
    [ObservableProperty]
    private FileItem? _selectedItem;
    
    /// <summary>
    /// Gets or sets whether there are unsaved changes.
    /// </summary>
    [ObservableProperty]
    private bool _hasChanges;
    
    /// <summary>
    /// Gets the collection of items.
    /// </summary>
    public ObservableCollection<FileItem> Items { get; } = new();
    
    #endregion
    
    #region Constructor
    
    public MyViewModel(
        IFileService fileService,
        IDialogService dialogService,
        AuditLoggingService auditService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }
    
    #endregion
    
    #region Commands
    
    /// <summary>
    /// Command to load data from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            Items.Clear();
            
            var files = await _fileService.GetFilesAsync("C:\\Data");
            
            foreach (var file in files)
            {
                Items.Add(file);
            }
            
            _auditService.LogSystemEvent("DATA_LOADED", $"Loaded {Items.Count} items");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to load data: {ex.Message}");
            _auditService.LogSystemEvent("DATA_LOAD_ERROR", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Command to save changes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            
            // Save logic here
            await Task.Delay(1000); // Simulate save
            
            HasChanges = false;
            _auditService.LogSystemEvent("DATA_SAVED", "Data saved successfully");
            
            await _dialogService.ShowInfoAsync("Success", "Data saved successfully");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to save: {ex.Message}");
            _auditService.LogSystemEvent("DATA_SAVE_ERROR", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private bool CanSave() => HasChanges && !IsLoading;
    
    #endregion
    
    #region Property Changed Handlers
    
    /// <summary>
    /// Called when SelectedItem changes.
    /// </summary>
    partial void OnSelectedItemChanged(FileItem? value)
    {
        // React to selection change
        if (value != null)
        {
            _auditService.LogSystemEvent("ITEM_SELECTED", $"Selected: {value.Name}");
        }
    }
    
    /// <summary>
    /// Called when HasChanges changes.
    /// </summary>
    partial void OnHasChangesChanged(bool value)
    {
        // Notify command to re-evaluate CanExecute
        SaveCommand.NotifyCanExecuteChanged();
    }
    
    #endregion
}
```

**Characteristics**:
- Inherits from `ObservableObject`
- Uses `[ObservableProperty]` for auto-implemented observable properties
- Uses `[RelayCommand]` for auto-implemented commands
- Depends on services via constructor injection
- Contains no UI element references
- Provides data and commands to View via data binding

---

### MVVM Benefits

| Benefit | Description |
|---------|-------------|
| **Separation of Concerns** | UI, presentation logic, and business logic are separated |
| **Testability** | ViewModels can be unit tested without UI |
| **Maintainability** | Changes to UI don't affect business logic |
| **Designer-Developer Workflow** | Designers work on XAML, developers on ViewModels |
| **Reusability** | ViewModels can be reused with different Views |

---

## Dependency Injection

**Dependency Injection (DI)** is a design pattern that implements Inversion of Control (IoC) for managing object dependencies.

### Configuration

**Location**: `App.xaml.cs` → `ConfigureServices()`

```csharp
using AIM.Services;
using AIM.ViewModels;
using AIM.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AIM;

public partial class App : Application
{
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // ============================================
        // SERVICES (Singleton - Shared Instance)
        // ============================================
        
        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();
        
        // Settings and Configuration
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IEncryptedSettingsService, EncryptedSettingsService>();
        
        // File Operations
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IDirectoryOperationService, DirectoryOperationService>();
        
        // Security and Encryption
        services.AddSingleton<SecurityService>();
        services.AddSingleton<EncryptionService>();
        
        // Audit and Logging
        services.AddSingleton<AuditLoggingService>();
        
        // UI Services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IInfoBarService, InfoBarService>();
        services.AddSingleton<IThemeService, ThemeService>();
        
        // Printing and Forms
        services.AddSingleton<IPrintService, PrintService>();
        services.AddSingleton<FormTemplateFactory>();
        
        // Messaging
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        
        // ============================================
        // VIEWMODELS (Transient - New Instance)
        // ============================================
        
        services.AddTransient<MainViewModel>();
        services.AddTransient<BrowseViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<LogViewerViewModel>();
        // ... other ViewModels
        
        // ============================================
        // PAGES (Transient - New Instance)
        // ============================================
        
        services.AddTransient<BrowsePage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<InventoryPage>();
        services.AddTransient<LogViewerPage>();
        // ... other Pages
        
        return services.BuildServiceProvider();
    }
}
```

### Service Lifetimes

| Lifetime | Description | Use Case |
|----------|-------------|----------|
| **Singleton** | Single instance for entire app lifetime | Services, state managers, repositories |
| **Transient** | New instance every time | ViewModels, Pages, temporary objects |
| **Scoped** | Single instance per scope | Not commonly used in WinUI apps |

### Dependency Resolution

**In ViewModels** (Constructor Injection):
```csharp
public class MyViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly SecurityService _securityService;
    
    // DI container automatically injects dependencies
    public MyViewModel(
        IFileService fileService,
        IDialogService dialogService,
        SecurityService securityService)
    {
        _fileService = fileService;
        _dialogService = dialogService;
        _securityService = securityService;
    }
}
```

**In Pages** (Service Locator Pattern):
```csharp
public sealed partial class MyPage : Page
{
    public MyViewModel ViewModel { get; }
    
    public MyPage()
    {
        this.InitializeComponent();
        
        // Resolve from DI container
        ViewModel = Ioc.Default.GetRequiredService<MyViewModel>();
        DataContext = ViewModel;
    }
}
```

**In Services** (Constructor Injection):
```csharp
public class FileService : IFileService
{
    private readonly AuditLoggingService _auditService;
    
    public FileService(AuditLoggingService auditService)
    {
        _auditService = auditService;
    }
}
```

### Benefits of DI

- **Loose Coupling**: Components depend on abstractions, not concrete implementations
- **Testability**: Easy to mock dependencies for unit testing
- **Maintainability**: Change implementations without affecting consumers
- **Flexibility**: Swap implementations easily (e.g., different storage backends)

---

## Service Layer Pattern

**Service Layer** encapsulates business logic into reusable, testable components.

### Service Structure

**Interface Definition**:
```csharp
namespace AIM.Services;

/// <summary>
/// Interface for file system operations.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Gets all files in the specified directory.
    /// </summary>
    Task<List<FileItem>> GetFilesAsync(string path);
    
    /// <summary>
    /// Gets all subdirectories in the specified directory.
    /// </summary>
    Task<List<DirectoryItem>> GetDirectoriesAsync(string path);
    
    /// <summary>
    /// Moves a file from source to destination.
    /// </summary>
    Task MoveFileAsync(string source, string destination);
    
    /// <summary>
    /// Copies a file from source to destination.
    /// </summary>
    Task CopyFileAsync(string source, string destination);
    
    /// <summary>
    /// Deletes a file.
    /// </summary>
    Task DeleteFileAsync(string path);
}
```

**Implementation**:
```csharp
namespace AIM.Services;

/// <summary>
/// Implementation of file system operations with audit logging.
/// </summary>
public class FileService : IFileService
{
    private readonly AuditLoggingService _auditService;
    
    public FileService(AuditLoggingService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }
    
    public async Task<List<FileItem>> GetFilesAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));
        
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        
        try
        {
            var files = new List<FileItem>();
            var directoryInfo = new DirectoryInfo(path);
            
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                files.Add(new FileItem
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    SizeBytes = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    Extension = fileInfo.Extension
                });
            }
            
            _auditService.LogDirectoryOperation(
                AuditActionTypes.DIR_ACCESS,
                path,
                $"Listed {files.Count} files"
            );
            
            return await Task.FromResult(files);
        }
        catch (UnauthorizedAccessException ex)
        {
            _auditService.LogSystemEvent("FILE_ACCESS_DENIED", $"Access denied to {path}");
            throw new InvalidOperationException($"Access denied to directory: {path}", ex);
        }
    }
    
    public async Task MoveFileAsync(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));
        
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Destination cannot be empty", nameof(destination));
        
        if (!File.Exists(source))
            throw new FileNotFoundException($"Source file not found: {source}");
        
        try
        {
            File.Move(source, destination);
            
            _auditService.LogMoveOperation(
                source,
                destination,
                Path.GetFileName(source)
            );
            
            await Task.CompletedTask;
        }
        catch (IOException ex)
        {
            _auditService.LogSystemEvent("FILE_MOVE_ERROR", ex.Message);
            throw new InvalidOperationException($"Failed to move file: {ex.Message}", ex);
        }
    }
    
    // ... other method implementations
}
```

### Service Design Principles

1. **Single Responsibility**: Each service has one clear purpose
2. **Interface-Based**: Always define an interface
3. **Dependency Injection**: Inject dependencies via constructor
4. **Stateless or Managed State**: Services don't hold request-specific state
5. **Exception Handling**: Catch and rethrow with meaningful messages
6. **Audit Logging**: Log important operations
7. **Async/Await**: Use async for I/O operations

---

## Repository Pattern

**Repository Pattern** abstracts data access, providing a collection-like interface for retrieving and persisting entities.

### Settings Repository Example

**Interface**:
```csharp
namespace AIM.Services;

public interface ISettingsService
{
    /// <summary>
    /// Loads application settings from storage.
    /// </summary>
    AppSettings LoadSettings();
    
    /// <summary>
    /// Saves application settings to storage.
    /// </summary>
    void SaveSettings(AppSettings settings);
}
```

**Implementation**:
```csharp
using AIM.Models;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace AIM.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    
    public SettingsService()
    {
        var appDataFolder = ApplicationData.Current.LocalFolder.Path;
        _settingsPath = Path.Combine(appDataFolder, "settings.json");
    }
    
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            // Log error and return defaults
            Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        
        return new AppSettings();
    }
    
    public void SaveSettings(AppSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving settings: {ex.Message}");
            throw new InvalidOperationException("Failed to save settings", ex);
        }
    }
}
```

### Benefits

- **Abstraction**: ViewModels don't know about storage mechanism
- **Testability**: Easy to create in-memory or mock repositories
- **Flexibility**: Can switch from JSON to database without affecting consumers

---

## Command Pattern

**Command Pattern** encapsulates requests as objects, allowing parameterization and queuing of requests.

### RelayCommand (CommunityToolkit.Mvvm)

**Simple Command**:
```csharp
public partial class MyViewModel : ObservableObject
{
    [RelayCommand]
    private void DoSomething()
    {
        // Command logic
        Debug.WriteLine("Command executed");
    }
}

// Generated code creates:
// public IRelayCommand DoSomethingCommand { get; }
```

**Async Command**:
```csharp
public partial class MyViewModel : ObservableObject
{
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await Task.Delay(1000);
        // Load data
    }
}

// Generated code creates:
// public IAsyncRelayCommand LoadDataCommand { get; }
```

**Command with Parameter**:
```csharp
public partial class MyViewModel : ObservableObject
{
    [RelayCommand]
    private void DeleteItem(FileItem item)
    {
        if (item != null)
        {
            Items.Remove(item);
        }
    }
}

// XAML binding:
// <Button Command="{x:Bind ViewModel.DeleteItemCommand}" 
//         CommandParameter="{x:Bind Item}"/>
```

**Command with CanExecute**:
```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _hasChanges;
    
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        // Save logic
    }
    
    private bool CanSave() => HasChanges;
    
    // Notify command when property changes
    partial void OnHasChangesChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }
}
```

### Benefits

- **Decoupling**: UI doesn't call ViewModel methods directly
- **Testability**: Commands can be tested independently
- **Reusability**: Commands can be bound to multiple UI elements
- **State Management**: CanExecute provides automatic UI state management

---

## Observer Pattern

**Observer Pattern** defines a one-to-many dependency where observers are notified of state changes.

### INotifyPropertyChanged

**Manual Implementation**:
```csharp
public class MyViewModel : INotifyPropertyChanged
{
    private string _name;
    
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

**Using ObservableObject (CommunityToolkit.Mvvm)**:
```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;
    
    // Automatically generates:
    // - Property with INotifyPropertyChanged
    // - OnNameChanging (before value changes)
    // - OnNameChanged (after value changes)
}
```

### ObservableCollection

**Usage**:
```csharp
public class MyViewModel : ObservableObject
{
    // Automatically notifies UI when items added/removed
    public ObservableCollection<FileItem> Files { get; } = new();
    
    public void AddFile(FileItem file)
    {
        Files.Add(file); // UI automatically updates
    }
    
    public void RemoveFile(FileItem file)
    {
        Files.Remove(file); // UI automatically updates
    }
    
    public void ClearFiles()
    {
        Files.Clear(); // UI automatically updates
    }
}
```

---

## Messaging Pattern

**Messaging Pattern** enables loosely-coupled communication between components using a message bus.

### WeakReferenceMessenger (CommunityToolkit.Mvvm)

**Define Message**:
```csharp
namespace AIM.Messages;

/// <summary>
/// Message sent when a print form is requested.
/// </summary>
public class PrintFormMessage
{
    public PrintableForm FormData { get; }
    
    public PrintFormMessage(PrintableForm formData)
    {
        FormData = formData ?? throw new ArgumentNullException(nameof(formData));
    }
}
```

**Send Message**:
```csharp
public class FormGeneratorViewModel : ObservableObject
{
    private readonly IMessenger _messenger;
    
    public FormGeneratorViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }
    
    [RelayCommand]
    private void PrintForm()
    {
        var formData = GenerateFormData();
        
        // Send message to any registered recipients
        _messenger.Send(new PrintFormMessage(formData));
    }
}
```

**Receive Message**:
```csharp
public class PrintableFormViewModel : ObservableObject
{
    private readonly IMessenger _messenger;
    
    public PrintableFormViewModel(IMessenger messenger)
    {
        _messenger = messenger;
        
        // Register to receive messages
        _messenger.Register<PrintFormMessage>(this, (recipient, message) =>
        {
            // Handle the message
            ProcessPrintRequest(message.FormData);
        });
    }
    
    private void ProcessPrintRequest(PrintableForm formData)
    {
        // Process the print request
        FormData = formData;
    }
}
```

**Unregister (Optional)**:
```csharp
public void Cleanup()
{
    _messenger.UnregisterAll(this);
}
```

### Benefits

- **Loose Coupling**: Sender and receiver don't know about each other
- **Flexibility**: Multiple receivers can listen to the same message
- **Testability**: Easy to test message handling independently

---

## Factory Pattern

**Factory Pattern** encapsulates object creation logic.

### FormTemplateFactory Example

**Factory Implementation**:
```csharp
namespace AIM.Services;

/// <summary>
/// Factory for creating form templates.
/// </summary>
public class FormTemplateFactory
{
    public IFormTemplate CreateTemplate(string templateType)
    {
        return templateType?.ToLowerInvariant() switch
        {
            "ohio" => new OhioInventoryTemplate(),
            "i&m" => new IMInventoryTemplate(),
            _ => throw new ArgumentException($"Unknown template type: {templateType}", nameof(templateType))
        };
    }
    
    public List<string> GetAvailableTemplates()
    {
        return new List<string> { "Ohio", "I&M" };
    }
}
```

**Usage**:
```csharp
public class FormGeneratorViewModel : ObservableObject
{
    private readonly FormTemplateFactory _templateFactory;
    
    public FormGeneratorViewModel(FormTemplateFactory templateFactory)
    {
        _templateFactory = templateFactory;
    }
    
    [RelayCommand]
    private void GenerateForm(string templateType)
    {
        var template = _templateFactory.CreateTemplate(templateType);
        var form = template.Generate(data);
        // ...
    }
}
```

---

## Strategy Pattern

**Strategy Pattern** defines a family of algorithms and makes them interchangeable.

### Search Strategy Example

**Strategy Interface**:
```csharp
public interface ISearchStrategy
{
    Task<List<FileItem>> SearchAsync(string root, string query);
}
```

**Concrete Strategies**:
```csharp
public class FilenameSearchStrategy : ISearchStrategy
{
    public async Task<List<FileItem>> SearchAsync(string root, string query)
    {
        var results = new List<FileItem>();
        
        foreach (var file in Directory.EnumerateFiles(root, $"*{query}*", SearchOption.AllDirectories))
        {
            results.Add(new FileItem 
            { 
                Name = Path.GetFileName(file),
                FullPath = file
            });
        }
        
        return await Task.FromResult(results);
    }
}

public class ContentSearchStrategy : ISearchStrategy
{
    public async Task<List<FileItem>> SearchAsync(string root, string query)
    {
        var results = new List<FileItem>();
        
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new FileItem 
                { 
                    Name = Path.GetFileName(file),
                    FullPath = file
                });
            }
        }
        
        return results;
    }
}
```

**Context (Service)**:
```csharp
public class SearchService : ISearchService
{
    private ISearchStrategy _strategy;
    
    public void SetStrategy(SearchType searchType)
    {
        _strategy = searchType switch
        {
            SearchType.Filename => new FilenameSearchStrategy(),
            SearchType.Content => new ContentSearchStrategy(),
            _ => throw new ArgumentException("Invalid search type")
        };
    }
    
    public async Task<List<FileItem>> SearchAsync(string root, string query)
    {
        if (_strategy == null)
            throw new InvalidOperationException("Search strategy not set");
        
        return await _strategy.SearchAsync(root, query);
    }
}
```

---

## Anti-Patterns to Avoid

### 1. God Object / God ViewModel

**❌ Anti-Pattern**:
```csharp
public class MainViewModel : ObservableObject
{
    // Too many responsibilities!
    public void LoadFiles() { }
    public void SearchFiles() { }
    public void PrintFiles() { }
    public void ManageSettings() { }
    public void HandleSecurity() { }
    public void GenerateForms() { }
    // ... 50 more methods
}
```

**✅ Solution**: Separate into focused ViewModels
```csharp
public class BrowseViewModel : ObservableObject
{
    public void LoadFiles() { }
}

public class SearchViewModel : ObservableObject
{
    public void SearchFiles() { }
}

public class SettingsViewModel : ObservableObject
{
    public void ManageSettings() { }
}
```

---

### 2. Static Service Locator

**❌ Anti-Pattern**:
```csharp
public class MyViewModel
{
    public void DoSomething()
    {
        var service = ServiceLocator.GetService<IFileService>(); // Hard dependency
        service.DoWork();
    }
}
```

**✅ Solution**: Use Constructor Injection
```csharp
public class MyViewModel
{
    private readonly IFileService _fileService;
    
    public MyViewModel(IFileService fileService)
    {
        _fileService = fileService;
    }
    
    public void DoSomething()
    {
        _fileService.DoWork();
    }
}
```

---

### 3. Business Logic in Code-Behind

**❌ Anti-Pattern**:
```csharp
public sealed partial class MyPage : Page
{
    public MyPage()
    {
        this.InitializeComponent();
    }
    
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // Business logic in code-behind!
        var files = Directory.GetFiles("C:\\Path");
        foreach (var file in files)
        {
            // Process files...
        }
    }
}
```

**✅ Solution**: Move logic to ViewModel
```csharp
// Code-behind (minimal)
public sealed partial class MyPage : Page
{
    public MyViewModel ViewModel { get; }
    
    public MyPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<MyViewModel>();
        DataContext = ViewModel;
    }
}

// ViewModel (business logic)
public partial class MyViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    
    [RelayCommand]
    private async Task ProcessFilesAsync()
    {
        var files = await _fileService.GetFilesAsync("C:\\Path");
        foreach (var file in files)
        {
            // Process files...
        }
    }
}
```

---

### 4. Tight Coupling to Concrete Classes

**❌ Anti-Pattern**:
```csharp
public class MyViewModel
{
    private FileService _fileService = new FileService(); // Tight coupling
    
    public void LoadData()
    {
        _fileService.GetFiles();
    }
}
```

**✅ Solution**: Depend on Abstractions
```csharp
public class MyViewModel
{
    private readonly IFileService _fileService;
    
    public MyViewModel(IFileService fileService)
    {
        _fileService = fileService; // Loose coupling via interface
    }
    
    public void LoadData()
    {
        _fileService.GetFiles();
    }
}
```

---

### 5. Magic Strings

**❌ Anti-Pattern**:
```csharp
_auditService.LogSystemEvent("DATA_LOADED", "Data loaded");
_auditService.LogSystemEvent("DATA_LOAD", "Data loaded"); // Typo!
```

**✅ Solution**: Use Constants
```csharp
public static class AuditActionTypes
{
    public const string DATA_LOADED = "DATA_LOADED";
    public const string DATA_SAVED = "DATA_SAVED";
}

_auditService.LogSystemEvent(AuditActionTypes.DATA_LOADED, "Data loaded");
```

---

### 6. Ignoring Async/Await

**❌ Anti-Pattern**:
```csharp
public void LoadData()
{
    var task = _service.GetDataAsync();
    task.Wait(); // Blocking call, can cause deadlocks
    var data = task.Result;
}
```

**✅ Solution**: Use Async/Await Properly
```csharp
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync(); // Non-blocking
}
```

---

### 7. Swallowing Exceptions

**❌ Anti-Pattern**:
```csharp
try
{
    DoSomething();
}
catch
{
    // Silent failure - user doesn't know what happened
}
```

**✅ Solution**: Handle Errors Properly
```csharp
try
{
    DoSomething();
}
catch (Exception ex)
{
    _auditService.LogSystemEvent("ERROR", ex.Message);
    await _dialogService.ShowErrorAsync("Error", $"Operation failed: {ex.Message}");
    // Or rethrow if appropriate
}
```

---

### 8. Not Using ObservableCollection

**❌ Anti-Pattern**:
```csharp
public List<FileItem> Files { get; set; } = new(); // Won't notify UI of changes

public void AddFile(FileItem file)
{
    Files.Add(file); // UI won't update!
}
```

**✅ Solution**: Use ObservableCollection
```csharp
public ObservableCollection<FileItem> Files { get; } = new();

public void AddFile(FileItem file)
{
    Files.Add(file); // UI automatically updates
}
```

---

## Conclusion

These design patterns work together to create a maintainable, testable, and extensible application architecture:

- **MVVM**: Separates UI from business logic
- **DI**: Provides loose coupling and testability
- **Service Layer**: Encapsulates business logic
- **Repository**: Abstracts data access
- **Command**: Decouples UI actions from logic
- **Observer**: Enables reactive UI updates
- **Messaging**: Enables cross-component communication
- **Factory**: Encapsulates object creation
- **Strategy**: Makes algorithms interchangeable

By following these patterns and avoiding anti-patterns, you'll create code that is easier to understand, test, and maintain.

For more information, see:
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture overview
- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines
- [README.md](README.md) - Project overview
