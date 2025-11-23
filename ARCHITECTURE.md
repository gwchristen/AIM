# AIM - Architecture Documentation

## Table of Contents

- [System Overview](#system-overview)
- [High-Level Architecture](#high-level-architecture)
- [Project Structure](#project-structure)
- [Design Patterns](#design-patterns)
- [Data Flow Diagrams](#data-flow-diagrams)
- [Service Layer](#service-layer)
- [ViewModels](#viewmodels)
- [Security Architecture](#security-architecture)
- [Audit Logging](#audit-logging)
- [Configuration Management](#configuration-management)
- [Extension Guide](#extension-guide)
- [Common Development Tasks](#common-development-tasks)

---

## System Overview

**AIM (Asset Inventory Management)** is a professional Windows desktop application built with **WinUI 3** and **.NET 8.0**. It provides comprehensive asset inventory management capabilities with enterprise-grade security, detailed audit logging, and a modern user interface.

### Technology Stack

- **UI Framework**: WinUI 3 (Windows App SDK)
- **Language**: C# 12
- **Target Framework**: .NET 8.0
- **Architecture Pattern**: MVVM (Model-View-ViewModel)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **MVVM Toolkit**: CommunityToolkit.Mvvm
- **Charting**: LiveCharts2
- **Logging**: Serilog

### Key Characteristics

- **Desktop-First**: Native Windows application with deep OS integration
- **Security-Focused**: Multi-layered authentication and encryption
- **Audit-Enabled**: Every action is logged with complete traceability
- **Extensible**: Modular architecture with clear separation of concerns
- **Responsive**: Modern, themeable UI with accessibility support

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  MainWindow  │  │    Pages     │  │   Controls   │          │
│  │   (Shell)    │  │ (XAML Views) │  │   (Custom)   │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                  │                  │                  │
│         └──────────────────┴──────────────────┘                  │
│                            │                                     │
│                    Data Binding (XAML)                          │
│                            │                                     │
└────────────────────────────┼─────────────────────────────────────┘
                             │
┌────────────────────────────┼─────────────────────────────────────┐
│                   VIEWMODEL LAYER (MVVM)                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ MainViewModel│  │BrowseViewModel│  │SettingsVM... │          │
│  │ (App State)  │  │ (Page Logic) │  │(Other Pages) │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                  │                  │                  │
│         └──────────────────┴──────────────────┘                  │
│                            │                                     │
│                    Service Injection                            │
│                            │                                     │
└────────────────────────────┼─────────────────────────────────────┘
                             │
┌────────────────────────────┼─────────────────────────────────────┐
│                      SERVICE LAYER                               │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────┐             │
│  │ Navigation  │ │   Security   │ │  ThemeService│             │
│  │   Service   │ │   Service    │ │              │             │
│  └─────────────┘ └──────────────┘ └──────────────┘             │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────┐             │
│  │    File     │ │   Settings   │ │ AuditLogging │             │
│  │   Service   │ │   Service    │ │   Service    │             │
│  └─────────────┘ └──────────────┘ └──────────────┘             │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────┐             │
│  │   Search    │ │  Encryption  │ │    Dialog    │             │
│  │   Service   │ │   Service    │ │   Service    │             │
│  └─────────────┘ └──────────────┘ └──────────────┘             │
└────────────────────────────┼─────────────────────────────────────┘
                             │
┌────────────────────────────┼─────────────────────────────────────┐
│                       DATA LAYER                                 │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────┐             │
│  │   Models    │ │  Encrypted   │ │  Audit Logs  │             │
│  │  (DTOs)     │ │   Settings   │ │   (JSON)     │             │
│  └─────────────┘ └──────────────┘ └──────────────┘             │
│  ┌─────────────┐ ┌──────────────┐                               │
│  │ File System │ │  App Config  │                               │
│  │   (I/O)     │ │   (JSON)     │                               │
│  └─────────────┘ └──────────────┘                               │
└──────────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

1. **Presentation Layer**: XAML views and user controls that display data and capture user input
2. **ViewModel Layer**: Business logic, state management, and command handling
3. **Service Layer**: Reusable business services with well-defined interfaces
4. **Data Layer**: Models, file I/O, and persistence mechanisms

---

## Project Structure

```
AIM/
├── App.xaml.cs                    # Application entry point, DI configuration
├── MainWindow.xaml(.cs)           # Main application shell with navigation
│
├── Views/                         # XAML pages and controls
│   ├── BrowsePage.xaml            # File browsing interface
│   ├── SearchPage.xaml            # Search functionality
│   ├── SettingsPage.xaml          # Application settings
│   ├── InventoryViewerPage.xaml   # Inventory viewing
│   ├── LogViewerPage.xaml         # Audit log viewer
│   ├── AdminTools/                # Administrative tools
│   │   ├── InventoryAdminToolsPage.xaml
│   │   ├── DirAnalysisPage.xaml
│   │   ├── BatchRenamerView.xaml
│   │   └── DirClonerView.xaml
│   └── PasswordInputControl.xaml  # Reusable password input control
│
├── ViewModels/                    # MVVM ViewModels
│   ├── MainViewModel.cs           # Main application state
│   ├── BrowseViewModel.cs         # Browse page logic
│   ├── SearchViewModel.cs         # Search logic
│   ├── SettingsViewModel.cs       # Settings management
│   ├── InventoryViewModel.cs      # Inventory operations
│   ├── LogViewerViewModel.cs      # Log viewing logic
│   └── ...                        # Other page ViewModels
│
├── Services/                      # Business services
│   ├── INavigationService.cs      # Navigation abstraction
│   ├── NavigationService.cs       # Frame navigation implementation
│   ├── SecurityService.cs         # Authentication & authorization
│   ├── EncryptionService.cs       # Data encryption (AES)
│   ├── EncryptedSettingsService.cs # Secure settings storage (DPAPI)
│   ├── AuditLoggingService.cs     # Audit trail logging
│   ├── ThemeService.cs            # Theme management
│   ├── IFileService.cs            # File operations abstraction
│   ├── FileService.cs             # File/directory operations
│   ├── ISearchService.cs          # Search abstraction
│   ├── SearchService.cs           # Content and filename search
│   ├── ISettingsService.cs        # Settings abstraction
│   ├── SettingsService.cs         # JSON settings persistence
│   ├── IDialogService.cs          # Dialog abstraction
│   ├── DialogService.cs           # WinUI dialog wrapper
│   ├── IPrintService.cs           # Printing abstraction
│   ├── PrintService.cs            # Form printing implementation
│   └── ...                        # Other services
│
├── Models/                        # Data models and DTOs
│   ├── AppSettings.cs             # Application settings model
│   ├── DirectoryItem.cs           # Directory tree node
│   ├── FileItem.cs                # File information
│   ├── ContentItem.cs             # File/folder content
│   ├── LogEntry.cs                # Audit log entry
│   ├── PrintableForm.cs           # Form generation models
│   └── ...                        # Other models
│
├── Converters/                    # XAML value converters
│   ├── BoolToVisibilityConverter.cs
│   ├── DateTimeConverter.cs
│   └── ...                        # Other converters
│
├── Messages/                      # MVVM messaging
│   └── PrintFormMessage.cs        # Cross-VM communication
│
├── Assets/                        # Application resources
│   └── (Icons, images, etc.)
│
├── Properties/
│   └── launchSettings.json
│
└── AIM.csproj                     # Project configuration
```

### Directory Purposes

| Directory | Purpose |
|-----------|---------|
| `Views/` | XAML pages and user controls - UI only, minimal code-behind |
| `ViewModels/` | Business logic, command handlers, observable properties |
| `Services/` | Reusable services with clear interfaces, injected into ViewModels |
| `Models/` | Plain C# objects (POCOs) for data transfer and state |
| `Converters/` | XAML value converters for data binding transformations |
| `Messages/` | Messages for loosely-coupled communication between ViewModels |
| `Assets/` | Static resources (images, icons, app manifest) |

---

## Design Patterns

### 1. MVVM (Model-View-ViewModel)

**AIM strictly follows the MVVM pattern** for separation of concerns:

- **Models**: Data structures without business logic (e.g., `DirectoryItem`, `FileItem`)
- **Views**: XAML files defining UI structure and appearance
- **ViewModels**: Bridge between Models and Views, containing presentation logic

**Benefits**:
- Clear separation of UI and business logic
- Testable business logic (ViewModels can be unit tested)
- Designer-developer workflow (XAML designers can work independently)
- Data binding eliminates manual UI updates

**Implementation**:
```csharp
// ViewModel inherits from ObservableObject (CommunityToolkit.Mvvm)
public partial class BrowseViewModel : ObservableObject
{
    // Observable properties automatically notify UI of changes
    [ObservableProperty]
    private string _rootName = string.Empty;
    
    // RelayCommand for button binding
    [RelayCommand]
    private async Task MoveFileAsync()
    {
        // Command logic here
    }
}
```

### 2. Dependency Injection (DI)

**AIM uses Microsoft.Extensions.DependencyInjection** for loose coupling and testability.

**Configuration** (`App.xaml.cs`):
```csharp
private static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    
    // Register services as singletons (shared instance)
    services.AddSingleton<INavigationService, NavigationService>();
    services.AddSingleton<ISettingsService, SettingsService>();
    services.AddSingleton<SecurityService>();
    
    // Register ViewModels as transient (new instance per request)
    services.AddTransient<MainViewModel>();
    services.AddTransient<BrowseViewModel>();
    
    return services.BuildServiceProvider();
}
```

**Injection** (ViewModels):
```csharp
public class BrowseViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;
    
    // Dependencies injected via constructor
    public BrowseViewModel(
        IFileService fileService,
        IDialogService dialogService)
    {
        _fileService = fileService;
        _dialogService = dialogService;
    }
}
```

**Benefits**:
- Loose coupling between components
- Easy to mock dependencies for testing
- Centralized service configuration
- Supports interface-based programming

### 3. Service Layer Pattern

**Services encapsulate reusable business logic** and are injected into ViewModels.

**Key Principles**:
- Services are defined by interfaces (`IFileService`, `INavigationService`)
- Services are stateless or manage their own state
- Services do not reference UI components
- Services can depend on other services

**Example**:
```csharp
public interface IFileService
{
    Task<List<FileItem>> GetFilesAsync(string path);
    Task MoveFileAsync(string source, string destination);
}

public class FileService : IFileService
{
    private readonly AuditLoggingService _auditService;
    
    public FileService(AuditLoggingService auditService)
    {
        _auditService = auditService;
    }
    
    public async Task MoveFileAsync(string source, string destination)
    {
        File.Move(source, destination);
        _auditService.LogFileOperation("MOVE", source, $"Moved to {destination}");
    }
}
```

### 4. Repository Pattern

**File and settings access is abstracted** behind service interfaces.

- `SettingsService`: Reads/writes JSON configuration files
- `EncryptedSettingsService`: Reads/writes encrypted security configuration
- `AuditLoggingService`: Appends to audit log file

This pattern allows:
- Easy switching of storage mechanisms
- Centralized data access logic
- Simplified testing with mock repositories

### 5. Messaging Pattern

**CommunityToolkit.Mvvm.Messaging enables loosely-coupled communication** between ViewModels.

**Example**:
```csharp
// Send message from one ViewModel
WeakReferenceMessenger.Default.Send(new PrintFormMessage(formData));

// Receive in another ViewModel
WeakReferenceMessenger.Default.Register<PrintFormMessage>(this, (r, m) =>
{
    // Handle the message
    ProcessPrintRequest(m.FormData);
});
```

**Use Cases**:
- Cross-page communication
- Event notification without tight coupling
- Avoiding service dependencies for simple notifications

---

## Data Flow Diagrams

### 1. Theme Selection Flow

```
User Action                  ViewModel              Service              System
    │                            │                      │                   │
    │   Clicks Theme Option      │                      │                   │
    ├──────────────────────────> │                      │                   │
    │                            │                      │                   │
    │                            │  SetTheme(theme)     │                   │
    │                            ├────────────────────> │                   │
    │                            │                      │                   │
    │                            │                      │  Apply Theme      │
    │                            │                      ├─────────────────> │
    │                            │                      │                   │
    │                            │                      │  Read Accent Color│
    │                            │                      │ <───────────────  │
    │                            │                      │                   │
    │                            │  SaveSettings()      │                   │
    │                            ├────────────────────> │                   │
    │                            │                      │  Write JSON       │
    │                            │                      ├─────────────────> │
    │                            │                      │                   │
    │                            │  ThemeChanged event  │                   │
    │                            │ <──────────────────  │                   │
    │                            │                      │                   │
    │   UI Updates (Data Bind)   │                      │                   │
    │ <──────────────────────── │                      │                   │
```

### 2. Security Authentication Flow

```
User Action              ViewModel          SecurityService      EncryptedSettings     AuditLog
    │                        │                      │                    │                 │
    │  Enters Password       │                      │                    │                 │
    ├──────────────────────> │                      │                    │                 │
    │                        │                      │                    │                 │
    │                        │  ValidatePassword()  │                    │                 │
    │                        ├────────────────────> │                    │                 │
    │                        │                      │                    │                 │
    │                        │                      │  LoadConfig()      │                 │
    │                        │                      ├──────────────────> │                 │
    │                        │                      │                    │                 │
    │                        │                      │  Decrypt (DPAPI)   │                 │
    │                        │                      │ <────────────────  │                 │
    │                        │                      │                    │                 │
    │                        │                      │  Hash & Compare    │                 │
    │                        │                      │                    │                 │
    │                        │                      │                    │   LogAuth()     │
    │                        │                      ├──────────────────────────────────────>│
    │                        │                      │                    │                 │
    │                        │  Result (Success)    │                    │                 │
    │                        │ <──────────────────  │                    │                 │
    │                        │                      │                    │                 │
    │   Update UI Visibility │                      │                    │                 │
    │ <────────────────────  │                      │                    │                 │
```

### 3. File Browsing Flow

```
User Action              BrowseViewModel      FileService       AuditLog        View
    │                           │                  │               │             │
    │  Selects Directory        │                  │               │             │
    ├─────────────────────────> │                  │               │             │
    │                           │                  │               │             │
    │                           │  GetFiles(path)  │               │             │
    │                           ├────────────────> │               │             │
    │                           │                  │               │             │
    │                           │                  │  File I/O     │             │
    │                           │                  │               │             │
    │                           │  List<FileItem>  │               │             │
    │                           │ <──────────────  │               │             │
    │                           │                  │               │             │
    │                           │  LogAccess()     │               │             │
    │                           ├────────────────────────────────> │             │
    │                           │                  │               │             │
    │                           │  Update Collection               │             │
    │                           │  (ObservableCollection)          │             │
    │                           │                  │               │             │
    │                           │                  │               │  Data Bind  │
    │                           ├──────────────────────────────────────────────> │
    │                           │                  │               │             │
    │   UI Displays Files       │                  │               │             │
    │ <─────────────────────────────────────────────────────────────────────────│
```

### 4. Search Flow

```
User Action            SearchViewModel      SearchService      FileSystem       View
    │                        │                    │                │             │
    │  Enters Query          │                    │                │             │
    ├──────────────────────> │                    │                │             │
    │                        │                    │                │             │
    │  Clicks Search         │                    │                │             │
    ├──────────────────────> │                    │                │             │
    │                        │                    │                │             │
    │                        │  SearchAsync()     │                │             │
    │                        ├──────────────────> │                │             │
    │                        │                    │                │             │
    │                        │                    │  Enumerate     │             │
    │                        │                    ├──────────────> │             │
    │                        │                    │                │             │
    │                        │                    │  Read Content  │             │
    │                        │                    │ <────────────  │             │
    │                        │                    │                │             │
    │                        │                    │  Match Query   │             │
    │                        │                    │                │             │
    │                        │  Results           │                │             │
    │                        │ <────────────────  │                │             │
    │                        │                    │                │             │
    │                        │  Update Results Collection          │             │
    │                        │                    │                │             │
    │                        │                    │                │  Data Bind  │
    │                        ├──────────────────────────────────────────────────>│
    │                        │                    │                │             │
    │   Results Displayed    │                    │                │             │
    │ <─────────────────────────────────────────────────────────────────────────│
```

---

## Service Layer

### Core Services

#### 1. NavigationService

**Purpose**: Manages page navigation within the application.

**Interface**: `INavigationService`

**Key Methods**:
- `Initialize(Frame frame)`: Sets the navigation frame
- `NavigateTo(Type pageType)`: Navigates to a page
- `NavigateTo(Type pageType, object parameter)`: Navigates with parameters
- `GoBack()`: Navigates to previous page

**Location**: `/Services/NavigationService.cs`

**Usage**:
```csharp
_navigationService.NavigateTo(typeof(BrowsePage));
```

---

#### 2. SecurityService

**Purpose**: Provides authentication, authorization, and access control.

**Responsibilities**:
- Master password validation
- Authorized user management
- Rate limiting (5 failed attempts = 15-minute lockout)
- First-time setup detection
- Master password override management

**Key Properties**:
- `IsFullyUnlocked`: Whether user has access to restricted features
- `IsMasterPasswordOverrideActive`: Whether master password bypass is active
- `IsFirstTimeSetup`: Whether initial password setup is needed
- `CurrentUserId`: Current Windows username

**Key Methods**:
- `InitializeAsync()`: Load encrypted security configuration
- `ValidatePasswordAsync(string password)`: Validate password attempt
- `SetInitialPasswordAsync(string password)`: Set first-time password
- `ActivateMasterPasswordOverride()`: Enable override mode
- `DeactivateMasterPasswordOverride()`: Disable override mode
- `AddAuthorizedUser(string username)`: Add user to whitelist
- `RemoveAuthorizedUser(string username)`: Remove user from whitelist

**Location**: `/Services/SecurityService.cs`

**Security Model**:
```
Two-Tier Authentication:
1. Master Password: Administrative override (temporary)
2. Authorized Users: Permanent user whitelist

Access Granted if:
  - Current Windows user is in authorized list, OR
  - Master password override is active
```

---

#### 3. EncryptedSettingsService

**Purpose**: Encrypts and decrypts sensitive security configuration.

**Interface**: `IEncryptedSettingsService`

**Key Methods**:
- `SaveSecurityConfigAsync(path, masterPassword, authorizedUsers)`: Encrypt and save
- `LoadSecurityConfigAsync(path)`: Decrypt and load
- `VerifyPasswordAsync(path, password)`: Verify without full load

**Encryption**: Windows Data Protection API (DPAPI) with `LOCAL=user` scope
- Machine-specific encryption
- User-specific encryption
- No keys stored in code or config

**Location**: `/Services/EncryptedSettingsService.cs`

---

#### 4. AuditLoggingService

**Purpose**: Records all user actions with complete traceability.

**Key Methods**:
- `LogFileOperation(type, path, description, details)`: Log file operations
- `LogDirectoryOperation(type, path, description)`: Log directory access
- `LogSecurityEvent(type, description, details)`: Log auth events
- `LogSystemEvent(type, description)`: Log system events
- `GetAllLogs()`: Retrieve all log entries

**Log Storage**: JSON file in `%LocalAppData%\AIM\Logs\audit_log.json`

**Log Entry Structure**:
```csharp
{
    "Timestamp": "2025-11-17T01:20:00Z",
    "UserId": "username",
    "ActionType": "FILE_MOVE",
    "Description": "Moved file 'document.pdf'",
    "TargetPath": "C:\\path\\to\\file.pdf",
    "Details": "{\"from\":\"...\", \"to\":\"...\"}"
}
```

**Location**: `/Services/AuditLoggingService.cs`

---

#### 5. ThemeService

**Purpose**: Manages application theme and appearance.

**Interface**: `IThemeService`

**Supported Themes**:
- `FollowSystem`: Auto-detect Windows theme
- `Light`: Force light theme
- `Dark`: Force dark theme
- `HighContrast`: Force high contrast

**Key Properties**:
- `CurrentTheme`: Active theme
- `AccentColor`: Windows accent color
- `IsHighContrast`: High contrast mode detection

**Key Methods**:
- `InitializeTheme()`: Load and apply saved theme
- `SetTheme(AppTheme theme)`: Change theme
- `DetectAccentColor()`: Read Windows accent color

**Location**: `/Services/ThemeService.cs`

---

#### 6. SettingsService

**Purpose**: Manages application settings persistence.

**Interface**: `ISettingsService`

**Key Methods**:
- `LoadSettings()`: Read settings from JSON
- `SaveSettings(AppSettings settings)`: Write settings to JSON

**Storage**: `%LocalAppData%\Microsoft.WinUI.3\AIM\settings.json`

**Location**: `/Services/SettingsService.cs`

---

#### 7. FileService

**Purpose**: File and directory operations with error handling.

**Interface**: `IFileService`

**Key Methods**:
- `GetFilesAsync(string path)`: List files in directory
- `GetDirectoriesAsync(string path)`: List subdirectories
- `MoveFileAsync(source, destination)`: Move file
- `CopyFileAsync(source, destination)`: Copy file
- `DeleteFileAsync(path)`: Delete file

**Location**: `/Services/FileService.cs`

---

#### 8. SearchService

**Purpose**: File content and filename searching.

**Interface**: `ISearchService`

**Key Methods**:
- `SearchByFilenameAsync(root, query)`: Search filenames
- `SearchByContentAsync(root, query)`: Search file contents
- `SearchAsync(root, query, searchType)`: Unified search

**Search Types**:
- Filename search (fast, metadata-only)
- Content search (slower, reads file contents)

**Location**: `/Services/SearchService.cs`

---

#### 9. DialogService

**Purpose**: Abstracts WinUI ContentDialog for ViewModels.

**Interface**: `IDialogService`

**Key Methods**:
- `ShowErrorAsync(title, message)`: Error dialog
- `ShowConfirmationAsync(title, message)`: Confirmation dialog
- `ShowInfoAsync(title, message)`: Information dialog

**Location**: `/Services/DialogService.cs`

---

#### 10. PrintService

**Purpose**: Generates and prints inventory forms.

**Interface**: `IPrintService`

**Key Methods**:
- `PrintFormAsync(PrintableForm form)`: Print a form
- `GenerateFormPages(PrintableForm form)`: Convert to printable pages

**Supported Templates**:
- Ohio Inventory Template
- I&M Inventory Template

**Location**: `/Services/PrintService.cs`

---

### Service Dependency Graph

```
ViewModels
    │
    ├─> NavigationService
    ├─> FileService ──────────────> AuditLoggingService
    ├─> SearchService
    ├─> DialogService
    ├─> SettingsService
    ├─> SecurityService ──────────> EncryptedSettingsService
    │                         ├───> AuditLoggingService
    │                         └───> SettingsService
    ├─> ThemeService ─────────────> SettingsService
    ├─> AuditLoggingService
    └─> PrintService ─────────────> FormTemplateFactory
```

---

## ViewModels

### MainViewModel

**Purpose**: Central application state management.

**Key Properties**:
- `SelectedRoot`: Current root directory path
- `IsInventoryTabVisible`: Visibility of restricted features
- `LeftTree`: Directory tree structure
- `SelectedScanFiles`: Selected files for operations

**Key Methods**:
- `UpdateInventoryTabVisibility()`: Refresh UI based on security status

**Location**: `/ViewModels/MainViewModel.cs`

---

### BrowseViewModel

**Purpose**: File browsing and navigation logic.

**Key Features**:
- Dual-pane file browser
- Breadcrumb navigation
- File operations (move, copy, delete)
- Undo/redo support
- Sorting and filtering

**Key Properties**:
- `LeftFilteredContents`: Left pane files/folders
- `RightFilteredContents`: Right pane files/folders
- `SelectedLeftDirectory`: Current left directory
- `SelectedRightDirectory`: Current right directory

**Key Commands**:
- `MoveFileCommand`: Move files
- `CopyFileCommand`: Copy files
- `DeleteFileCommand`: Delete files
- `UndoCommand`: Undo last operation

**Location**: `/ViewModels/BrowseViewModel.cs`

---

### SettingsViewModel

**Purpose**: Application settings management.

**Key Features**:
- Theme selection
- Root directory configuration
- Security settings (master password, authorized users)
- Master password override toggle

**Key Properties**:
- `SelectedTheme`: Current theme
- `DefaultRootDirectory`: Default directory
- `AuthorizedUsers`: List of authorized users
- `IsMasterPasswordOverrideActive`: Override status

**Key Commands**:
- `SaveSettingsCommand`: Save configuration
- `SetMasterPasswordCommand`: Update password
- `AddAuthorizedUserCommand`: Add user
- `RemoveAuthorizedUserCommand`: Remove user
- `ToggleMasterPasswordOverrideCommand`: Toggle override

**Location**: `/ViewModels/SettingsViewModel.cs`

---

### SearchViewModel

**Purpose**: File search functionality.

**Key Features**:
- Filename and content search
- Search result display
- Navigation to search results

**Key Properties**:
- `SearchQuery`: User search query
- `SearchResults`: Found files
- `IsSearching`: Loading indicator
- `SearchType`: Filename vs. content search

**Key Commands**:
- `SearchCommand`: Execute search
- `ClearSearchCommand`: Clear results
- `OpenFileCommand`: Open search result

**Location**: `/ViewModels/SearchViewModel.cs`

---

### LogViewerViewModel

**Purpose**: Audit log viewing and filtering.

**Key Features**:
- View all audit logs
- Filter by action type
- Filter by date range
- Export logs

**Key Properties**:
- `LogEntries`: All log entries
- `FilteredLogEntries`: Filtered results
- `SelectedActionType`: Filter criteria

**Key Commands**:
- `RefreshLogsCommand`: Reload logs
- `FilterLogsCommand`: Apply filters
- `ExportLogsCommand`: Export to file

**Location**: `/ViewModels/LogViewerViewModel.cs`

---

### ViewModel Lifecycle

```
1. ViewModel Created (DI Container)
   ↓
2. Constructor Injection (Services)
   ↓
3. Initialize Commands (RelayCommand)
   ↓
4. Page Navigation (NavigationService)
   ↓
5. OnNavigatedTo (Optional, if implemented)
   ↓
6. User Interactions (Commands, Property Changes)
   ↓
7. Data Binding Updates (ObservableObject)
   ↓
8. Page Unloaded (ViewModel may be disposed)
```

---

## Security Architecture

### Multi-Layered Security Model

```
┌─────────────────────────────────────────────────────────────┐
│                   APPLICATION ACCESS LAYER                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Is Current User Authorized?                          │  │
│  │  ┌──────────────────────┬─────────────────────────┐  │  │
│  │  │  Authorized Users    │  Master Password        │  │  │
│  │  │  List Check          │  Override Active?       │  │  │
│  │  └──────────────────────┴─────────────────────────┘  │  │
│  │                  │                 │                  │  │
│  │                  └────────┬────────┘                  │  │
│  │                           ↓                           │  │
│  │                    GRANT ACCESS                       │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│                  PASSWORD VALIDATION LAYER                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  1. Load Encrypted Config (DPAPI Decrypt)            │  │
│  │  2. Hash Input Password (SHA-256)                    │  │
│  │  3. Compare Hash with Stored Hash                    │  │
│  │  4. Check Rate Limiting (5 attempts = 15 min lockout)│  │
│  │  5. Log Authentication Attempt (Audit)               │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│                   ENCRYPTION LAYER (DPAPI)                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Windows Data Protection API                         │  │
│  │  - Machine-specific encryption                       │  │
│  │  - User-specific encryption                          │  │
│  │  - No hardcoded keys                                 │  │
│  │  - Cannot decrypt on different machine/user         │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│                    STORAGE LAYER                             │
│  security.config (encrypted JSON):                          │
│  {                                                           │
│    "masterPasswordHash": "SHA-256 hash",                    │
│    "authorizedUsers": ["user1", "user2"],                   │
│    "encryptedData": "DPAPI-encrypted blob",                 │
│    "lastModified": "2025-11-17T01:20:00Z"                   │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
```

### Password Requirements

- **Minimum Length**: 8 characters
- **Uppercase**: At least 1 uppercase letter
- **Lowercase**: At least 1 lowercase letter
- **Digits**: At least 1 number
- **Symbols**: At least 1 special character

**Enforcement**: `PasswordValidator.ValidatePassword()`

### Rate Limiting

- **Max Failed Attempts**: 5
- **Lockout Duration**: 15 minutes
- **Lockout Reset**: After duration expires or app restart
- **Logging**: All attempts logged to audit log

### Encryption Flow

```
┌──────────────────┐
│  Plain Password  │
│  + User List     │
└────────┬─────────┘
         │
         ↓
┌────────────────────────┐
│  Serialize to JSON     │
└────────┬───────────────┘
         │
         ↓
┌────────────────────────┐
│  DPAPI Encrypt         │
│  (LOCAL=user scope)    │
└────────┬───────────────┘
         │
         ↓
┌────────────────────────┐
│  Base64 Encode         │
└────────┬───────────────┘
         │
         ↓
┌────────────────────────┐
│  Store in JSON         │
│  (encryptedData field) │
└────────────────────────┘
```

### First-Time Setup Flow

```
1. Application Launch
   ↓
2. Check if security.config exists
   ↓ (No)
3. Set IsFirstTimeSetup = true
   ↓
4. Show Welcome Dialog (MainWindow)
   ↓
5. User enters password + confirmation
   ↓
6. Validate password requirements
   ↓
7. Encrypt and save password
   ↓
8. Set IsFirstTimeSetup = false
   ↓
9. Continue to main application
```

---

## Audit Logging

### What is Logged?

| Event Type | Examples |
|------------|----------|
| **File Operations** | Move, copy, delete, rename |
| **Directory Operations** | Access, browse, create, delete |
| **Security Events** | Login, logout, password change, user add/remove |
| **System Events** | Theme change, settings save, app launch |

### Log Entry Structure

```csharp
public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; }          // Windows username
    public string ActionType { get; set; }      // FILE_MOVE, DIR_ACCESS, etc.
    public string Description { get; set; }     // Human-readable description
    public string TargetPath { get; set; }      // File/directory affected
    public string Details { get; set; }         // JSON-serialized details
}
```

### Log Storage

- **Location**: `%LocalAppData%\AIM\Logs\audit_log.json`
- **Format**: JSON array (one entry per line for efficient append)
- **Rotation**: Manual (not automatic)
- **Size**: Grows unbounded (consider implementing rotation)

### Example Log Entries

```json
{
  "Timestamp": "2025-11-17T01:20:30.123Z",
  "UserId": "john.doe",
  "ActionType": "FILE_MOVE",
  "Description": "Moved file 'report.pdf' from Documents to Archive",
  "TargetPath": "C:\\Users\\john.doe\\Documents\\report.pdf",
  "Details": "{\"from\":\"C:\\\\Users\\\\john.doe\\\\Documents\",\"to\":\"C:\\\\Archive\"}"
}
```

### Audit Action Types

Defined in `AuditActionTypes` class:
- `FILE_MOVE`, `FILE_COPY`, `FILE_DELETE`, `FILE_RENAME`
- `DIR_ACCESS`, `DIR_CREATE`, `DIR_DELETE`
- `AUTH_LOGIN_SUCCESS`, `AUTH_LOGIN_FAILURE`, `AUTH_PASSWORD_CHANGE`
- `USER_ADDED`, `USER_REMOVED`
- `SETTINGS_CHANGED`, `THEME_CHANGED`

### Viewing Audit Logs

Use `LogViewerPage` (Navigation → Log Viewer) to:
- View all logs in chronological order
- Filter by action type
- Filter by date range
- Search by user or path
- Export to CSV/JSON

---

## Configuration Management

### Application Settings (`AppSettings`)

**File**: `settings.json` in `%LocalAppData%\Microsoft.WinUI.3\AIM\`

**Structure**:
```json
{
  "DefaultRootDirectory": "C:\\AssetInventory",
  "Theme": "FollowSystem",
  "AuthorizedUsers": ["user1", "user2"],
  "LastOpenedPaths": ["C:\\Path1", "C:\\Path2"],
  "FormTemplateType": "Ohio"
}
```

**Properties**:
- `DefaultRootDirectory`: Default starting directory
- `Theme`: Selected theme (FollowSystem, Light, Dark, HighContrast)
- `AuthorizedUsers`: List of Windows usernames with access
- `LastOpenedPaths`: Recently accessed directories
- `FormTemplateType`: Default form template

**Access**:
```csharp
var settings = _settingsService.LoadSettings();
settings.Theme = "Dark";
_settingsService.SaveSettings(settings);
```

---

### Security Configuration (`security.config`)

**File**: `security.config` in `%LocalAppData%\AIM\Security\`

**Structure** (encrypted):
```json
{
  "masterPasswordHash": "SHA-256 hash of password",
  "authorizedUsers": ["user1", "user2"],
  "encryptedData": "Base64-encoded DPAPI-encrypted blob",
  "lastModified": "2025-11-17T01:20:00Z"
}
```

**Access**:
```csharp
await _encryptedSettingsService.SaveSecurityConfigAsync(
    configPath,
    masterPassword,
    authorizedUsers
);

var data = await _encryptedSettingsService.LoadSecurityConfigAsync(configPath);
```

---

### Dependency Injection Configuration

**File**: `App.xaml.cs` → `ConfigureServices()`

**Service Lifetimes**:
- **Singleton**: Single instance for entire app lifetime
  - Services: Navigation, Settings, Security, Theme, Audit, etc.
- **Transient**: New instance per request
  - ViewModels: All ViewModels
  - Pages: All Pages

**Adding a New Service**:
```csharp
// In ConfigureServices()
services.AddSingleton<IMyService, MyService>();
```

---

## Extension Guide

### Adding a New Page

**1. Create the Model (if needed)**

`/Models/MyDataModel.cs`:
```csharp
namespace AIM.Models;

public class MyDataModel
{
    public string Name { get; set; }
    public string Value { get; set; }
}
```

**2. Create the ViewModel**

`/ViewModels/MyPageViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIM.ViewModels;

public partial class MyPageViewModel : ObservableObject
{
    private readonly IMyService _myService;
    
    [ObservableProperty]
    private string _title = "My Page";
    
    public MyPageViewModel(IMyService myService)
    {
        _myService = myService;
    }
    
    [RelayCommand]
    private async Task DoSomethingAsync()
    {
        // Command logic
    }
}
```

**3. Create the View**

`/Views/MyPage.xaml`:
```xml
<Page
    x:Class="AIM.Views.MyPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:AIM.ViewModels">
    
    <Grid>
        <TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />
        <Button Content="Do Something" Command="{x:Bind ViewModel.DoSomethingCommand}" />
    </Grid>
</Page>
```

`/Views/MyPage.xaml.cs`:
```csharp
using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views;

public sealed partial class MyPage : Page
{
    public MyPageViewModel ViewModel { get; }
    
    public MyPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<MyPageViewModel>();
        DataContext = ViewModel;
    }
}
```

**4. Register in DI Container**

`App.xaml.cs` → `ConfigureServices()`:
```csharp
// ViewModels
services.AddTransient<MyPageViewModel>();

// Pages
services.AddTransient<MyPage>();
```

**5. Add Navigation Menu Item**

`MainWindow.xaml`:
```xml
<NavigationViewItem Content="My Page" Tag="MyPage" Icon="Page" />
```

`MainWindow.xaml.cs` → `NavigateToPage()`:
```csharp
Type? pageType = navItemTag switch
{
    // ... existing cases
    "MyPage" => typeof(MyPage),
    _ => null
};
```

---

### Adding a New Service

**1. Define Interface**

`/Services/IMyService.cs`:
```csharp
namespace AIM.Services;

public interface IMyService
{
    Task<string> GetDataAsync();
    void SaveData(string data);
}
```

**2. Implement Service**

`/Services/MyService.cs`:
```csharp
namespace AIM.Services;

public class MyService : IMyService
{
    private readonly AuditLoggingService _auditService;
    
    public MyService(AuditLoggingService auditService)
    {
        _auditService = auditService;
    }
    
    public async Task<string> GetDataAsync()
    {
        _auditService.LogSystemEvent("DATA_ACCESS", "Data retrieved");
        // Implementation
        return await Task.FromResult("Data");
    }
    
    public void SaveData(string data)
    {
        _auditService.LogSystemEvent("DATA_SAVE", $"Data saved: {data}");
        // Implementation
    }
}
```

**3. Register in DI Container**

`App.xaml.cs` → `ConfigureServices()`:
```csharp
services.AddSingleton<IMyService, MyService>();
```

**4. Inject into ViewModels**

```csharp
public class MyPageViewModel : ObservableObject
{
    private readonly IMyService _myService;
    
    public MyPageViewModel(IMyService myService)
    {
        _myService = myService;
    }
}
```

---

### Adding a New Theme

**1. Define Theme Enum Value**

`/Services/ThemeService.cs`:
```csharp
public enum AppTheme
{
    FollowSystem,
    Light,
    Dark,
    HighContrast,
    CustomTheme  // Add new theme
}
```

**2. Implement Theme Application Logic**

`/Services/ThemeService.cs` → `ApplyTheme()`:
```csharp
private void ApplyTheme()
{
    var rootElement = ((App)Application.Current).MainWindow.Content as FrameworkElement;
    if (rootElement == null) return;
    
    switch (CurrentTheme)
    {
        // ... existing cases
        case AppTheme.CustomTheme:
            rootElement.RequestedTheme = ElementTheme.Dark; // Or custom logic
            // Apply custom resources
            break;
    }
}
```

**3. Add to Settings UI**

`/Views/SettingsPage.xaml`:
```xml
<RadioButton Content="Custom Theme" IsChecked="{x:Bind ViewModel.IsCustomThemeSelected, Mode=TwoWay}" />
```

---

### Adding Audit Logging to New Features

**Pattern**:
```csharp
public class MyViewModel : ObservableObject
{
    private readonly AuditLoggingService _auditService;
    
    [RelayCommand]
    private async Task DoSomethingAsync()
    {
        // Perform action
        await PerformActionAsync();
        
        // Log the action
        _auditService.LogSystemEvent(
            "MY_ACTION_TYPE",
            $"Action performed with parameter: {parameter}"
        );
    }
}
```

**Define Action Types**:
Add to `AuditActionTypes` class:
```csharp
public static class AuditActionTypes
{
    // ... existing types
    public const string MY_NEW_ACTION = "MY_NEW_ACTION";
}
```

---

## Common Development Tasks

### Task 1: Adding a New Feature

**Example**: Add a "File Statistics" feature

**Steps**:

1. **Create Model** (`/Models/FileStatistics.cs`):
```csharp
public class FileStatistics
{
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public string LargestFile { get; set; }
}
```

2. **Create Service** (`/Services/IFileStatisticsService.cs` and implementation):
```csharp
public interface IFileStatisticsService
{
    Task<FileStatistics> CalculateStatisticsAsync(string path);
}
```

3. **Create ViewModel** (`/ViewModels/FileStatisticsViewModel.cs`):
```csharp
public partial class FileStatisticsViewModel : ObservableObject
{
    private readonly IFileStatisticsService _statsService;
    
    [ObservableProperty]
    private FileStatistics _statistics;
    
    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        Statistics = await _statsService.CalculateStatisticsAsync(path);
    }
}
```

4. **Create View** (`/Views/FileStatisticsPage.xaml`):
```xml
<Page>
    <StackPanel>
        <TextBlock Text="{x:Bind ViewModel.Statistics.TotalFiles, Mode=OneWay}" />
        <Button Command="{x:Bind ViewModel.LoadStatisticsCommand}" />
    </StackPanel>
</Page>
```

5. **Register in DI**:
```csharp
services.AddSingleton<IFileStatisticsService, FileStatisticsService>();
services.AddTransient<FileStatisticsViewModel>();
services.AddTransient<FileStatisticsPage>();
```

6. **Add Navigation**:
Update `MainWindow.xaml` and `MainWindow.xaml.cs`.

7. **Add Audit Logging**:
```csharp
_auditService.LogSystemEvent("STATS_CALCULATED", $"Statistics for {path}");
```

---

### Task 2: Testing a Service

**Pattern**:

Since this is a Windows-only WinUI app without a test project, testing is typically done manually or through Windows-specific test frameworks.

**Manual Testing Checklist**:
- [ ] Positive scenarios (happy path)
- [ ] Negative scenarios (error cases)
- [ ] Edge cases (empty data, null values)
- [ ] Security scenarios (unauthorized access)
- [ ] UI updates (data binding)
- [ ] Audit logging (verify logs created)

**Recommended Testing Approach**:
1. Create a test harness page in the app (debug-only)
2. Add buttons to trigger service methods
3. Display results in UI
4. Check audit logs after each action

---

### Task 3: Debugging MVVM Bindings

**Common Issues**:

1. **Binding not updating**:
   - Ensure property uses `[ObservableProperty]` or implements `INotifyPropertyChanged`
   - Use `Mode=OneWay` or `Mode=TwoWay` as appropriate
   - Check `DataContext` is set correctly

2. **Command not executing**:
   - Ensure method has `[RelayCommand]` attribute
   - Check `CanExecute` logic (if any)
   - Verify command is bound correctly in XAML

3. **Data not appearing**:
   - Check `x:Bind` path is correct
   - Verify ViewModel property is public
   - Use `Mode=OneWay` for read-only data

**Debugging Tools**:
- **Debug.WriteLine()**: Output to Visual Studio Output window
- **Breakpoints**: In ViewModel methods and property setters
- **Live Visual Tree**: Inspect UI element hierarchy and DataContext
- **Live Property Explorer**: View binding errors in real-time

---

### Task 4: Implementing a Complex Workflow

**Example**: Multi-step file archiving workflow

**Pattern**:

1. **Break into States**:
```csharp
public enum ArchiveWorkflowState
{
    SelectFiles,
    ConfigureOptions,
    Processing,
    Complete
}
```

2. **Track Current State**:
```csharp
[ObservableProperty]
private ArchiveWorkflowState _currentState = ArchiveWorkflowState.SelectFiles;
```

3. **Create State-Specific Commands**:
```csharp
[RelayCommand]
private void NextStep()
{
    CurrentState = CurrentState switch
    {
        ArchiveWorkflowState.SelectFiles => ArchiveWorkflowState.ConfigureOptions,
        ArchiveWorkflowState.ConfigureOptions => ArchiveWorkflowState.Processing,
        // ...
        _ => CurrentState
    };
}
```

4. **Bind UI Visibility to State**:
```xml
<StackPanel Visibility="{x:Bind ViewModel.IsSelectFilesVisible, Mode=OneWay}">
    <!-- Step 1 UI -->
</StackPanel>
```

5. **Log Each Step**:
```csharp
_auditService.LogSystemEvent(
    "ARCHIVE_STEP",
    $"Archive workflow: {CurrentState}"
);
```

---

### Task 5: Handling Errors Gracefully

**Pattern**:

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    try
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        var data = await _dataService.LoadAsync();
        Data = data;
        
        _auditService.LogSystemEvent("DATA_LOADED", "Data loaded successfully");
    }
    catch (FileNotFoundException ex)
    {
        ErrorMessage = "File not found. Please check the path.";
        await _dialogService.ShowErrorAsync("Error", ErrorMessage);
        _auditService.LogSystemEvent("DATA_LOAD_ERROR", ex.Message);
    }
    catch (UnauthorizedAccessException ex)
    {
        ErrorMessage = "Access denied. Please check permissions.";
        await _dialogService.ShowErrorAsync("Error", ErrorMessage);
        _auditService.LogSystemEvent("DATA_LOAD_ERROR", ex.Message);
    }
    catch (Exception ex)
    {
        ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        await _dialogService.ShowErrorAsync("Error", ErrorMessage);
        _auditService.LogSystemEvent("DATA_LOAD_ERROR", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}
```

**Best Practices**:
- Always show user-friendly error messages
- Log errors to audit log
- Use `finally` to reset loading states
- Provide recovery options when possible

---

## Additional Resources

### Documentation References

- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines
- [DESIGN_PATTERNS.md](DESIGN_PATTERNS.md) - Detailed design pattern examples
- [README.md](README.md) - Project overview and features

### External Documentation

- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [.NET Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Windows Data Protection API (DPAPI)](https://docs.microsoft.com/en-us/windows/win32/api/dpapi/)

### Code Examples

All architecture patterns demonstrated in this document can be found in the actual codebase:
- Service examples: `/Services/`
- ViewModel examples: `/ViewModels/`
- View examples: `/Views/`
- Model examples: `/Models/`

---

## Conclusion

This architecture documentation provides a comprehensive overview of the AIM application's design, patterns, and extension points. By following the MVVM pattern with dependency injection, the codebase remains modular, testable, and maintainable.

**Key Takeaways**:
- **MVVM**: Clear separation between UI and business logic
- **DI**: Loose coupling and easy testing
- **Services**: Reusable, single-responsibility components
- **Security**: Multi-layered with encryption and audit logging
- **Extensibility**: Well-defined patterns for adding features

For specific implementation examples, refer to the existing codebase and the DESIGN_PATTERNS.md document.
