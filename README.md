# AIM - Archive Inventory Management

A cross-platform desktop application for managing archive inventories, built with Avalonia UI.

## Features

- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Modern UI**: Fluent design with light/dark theme support
- **Navigation**: Side panel navigation with icons
- **File Management**: Browse, search, preview, and manage files
- **Statistics**: View inventory statistics and problematic files
- **Scans**: Manage file scans
- **Settings**: Configure application settings

## Technology Stack

- **Framework**: .NET 8.0
- **UI Framework**: Avalonia 11.2.2
- **MVVM**: CommunityToolkit.Mvvm
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog

## Building and Running

### Prerequisites

- .NET 8.0 SDK or later

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

## Project Structure

- `Views/` - UI pages (Browse, Preview, Search, Scans, Stats, Settings)
- `ViewModels/` - View models for MVVM pattern
- `Models/` - Data models
- `Services/` - Business logic services
- `Converters/` - Value converters for data binding

## Converting from WinUI 3

This application was originally built with WinUI 3 and has been converted to Avalonia for cross-platform support. Key changes include:

- Replaced WinUI controls with Avalonia equivalents
- Updated XAML namespaces
- Converted Frame navigation to ContentControl
- Updated converters to use Avalonia APIs
- Replaced DispatcherQueue with Avalonia.Threading.Dispatcher

## License

(Add your license here)
