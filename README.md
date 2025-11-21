# AIM - Asset Inventory Management

<div align="center">

![Status](https://img.shields.io/badge/status-Active-brightgreen)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-purple)
![WinUI 3](https://img.shields.io/badge/WinUI-3-blue)

A professional Windows desktop application for comprehensive asset inventory management with enterprise-grade security, audit logging, and customizable theming.

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Security Model](#security-model)
- [Configuration](#configuration)
- [Usage Guide](#usage-guide)
- [Development](#development)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

**AIM (Asset Inventory Management)** is a robust Windows desktop application built with WinUI 3 and C#. It provides organizations with a comprehensive solution for managing, tracking, and auditing their asset inventory. With enterprise-grade security, detailed audit logging, and a modern user interface, AIM ensures complete transparency and control over your organization's assets.

### Key Highlights

- 🔒 **Enterprise Security** - Master password override, role-based access control, and authorized user management
- 📊 **Comprehensive Auditing** - Every action is logged with timestamp, user, and details
- 🎨 **Modern Theming** - Support for Light, Dark, High Contrast, and system-following themes
- 📁 **Directory Operations** - Browse, analyze, and manage directory structures
- 🔍 **Advanced Search** - Search by filename or file content across your inventory
- 📝 **Form Generation** - Create, customize, and print inventory forms
- 💾 **Encrypted Settings** - Security configuration stored securely with AES encryption

---

## Features

### Core Features

| Feature | Description |
|---------|-------------|
| **Asset Browsing** | Navigate and explore directory structures with an intuitive tree view |
| **File Management** | View, preview, and manage files within your inventory system |
| **Advanced Search** | Full-text search across filenames and file contents |
| **Inventory Management** | Organize, archive, and track inventory with detailed metadata |
| **Form Generation** | Generate printable forms with customizable templates (Ohio, I&M) |
| **Batch Operations** | Clone directories, rename files, and perform batch operations |
| **Directory Analysis** | Analyze directory structures and generate reports |

### Security Features

| Feature | Description |
|---------|-------------|
| **Database-Centric Security** | Centralized SQLite database for user management |
| **Role-Based Access Control** | Three access levels: Basic, Admin, and SuperAdmin |
| **Real-Time Synchronization** | User changes propagate across all instances within 30 seconds |
| **Audit Logging** | Complete audit trail of all system and security activities |
| **No Passwords Required** | Windows authentication eliminates password management |

### User Experience Features

| Feature | Description |
|---------|-------------|
| **Light Theme** | Bright, clean interface for well-lit environments |
| **Dark Theme** | Eye-friendly interface for low-light conditions |
| **High Contrast Mode** | Enhanced visibility for accessibility needs |
| **System Theme Sync** | Automatically follows Windows theme settings |
| **Windows Accent Color** | Uses your system accent color throughout the app |

---

## System Requirements

### Minimum Requirements

- **OS**: Windows 10 (Build 19041) or Windows 11
- **Runtime**: .NET 8.0 or later
- **RAM**: 2 GB minimum (4 GB recommended)
- **Disk Space**: 500 MB for installation
- **Display**: 1366 x 768 resolution minimum

### Development Requirements

- **Visual Studio 2022** or later
- **.NET 8.0 SDK**
- **Windows App SDK (WinUI 3)** workload installed
- **C# 12** or later support

---

## Installation

### From Released Build (Recommended)

1. Download the latest `AIM-Installer-win-x64.exe` from the [Releases](https://github.com/gwchristen/AIM/releases) page
2. Run the installer and follow the on-screen wizard:
   - Choose installation directory
   - Configure optional shared security
   - Select shortcut options
3. Launch AIM from your Start Menu or Desktop shortcut

The installer is a single self-contained EXE with no external dependencies required.

### Building the Installer

To build the installer from source:

```powershell
# Build the installer (requires Windows)
.\Build-Installer.ps1

# Or specify configuration and runtime
.\Build-Installer.ps1 -Configuration Release -Runtime win-x64
```

For detailed instructions, see [README-INSTALLER.md](README-INSTALLER.md).

### From Source Code

1. **Clone the repository**:
   ```bash
   git clone https://github.com/gwchristen/AIM.git
   cd AIM
   ```

2. **Open the solution in Visual Studio 2022**:
   - Open `AIM.sln`

3. **Restore NuGet packages**:
   - Visual Studio will automatically restore packages on first build

4. **Build the solution**:
   - Press `Ctrl+Shift+B` or select **Build → Build Solution**

5. **Run the application**:
   - Press `F5` to start debugging

---

## Quick Start

### First Launch

On first launch after installation:
1. AIM automatically detects your Windows username
2. Verifies you are authorized in the security database
3. If you are the installer user, you have SuperAdmin access
4. If added by an Admin, you have the assigned access level
5. No password required - Windows authentication only

### Basic Usage

1. **Browse Files**:
   - Navigate to **Browse** from the left menu
   - Select a root directory to explore
   - Use the dual-pane interface to browse and manage files

2. **Search Files**:
   - Navigate to **Search**
   - Choose between filename or content search
   - Enter your search query and click **Search**

3. **Configure Settings**:
   - Click the **Settings** gear icon in the navigation
   - Configure theme, default directories, and security settings

4. **View Audit Logs**:
   - Navigate to **Log Viewer**
   - Review all system activities with timestamps and user information

---

## Architecture

AIM is built using modern design patterns and best practices:

- **MVVM Pattern**: Clear separation between UI and business logic
- **Dependency Injection**: Loose coupling using Microsoft.Extensions.DependencyInjection
- **Service Layer**: Reusable business services with well-defined interfaces
- **Repository Pattern**: Abstracted data access for settings and configuration
- **Command Pattern**: UI commands bound to ViewModel logic

For detailed architecture documentation, see:
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Comprehensive system design, data flow diagrams, and extension guide
- **[DESIGN_PATTERNS.md](DESIGN_PATTERNS.md)** - Detailed design pattern implementations with code examples

### Technology Stack

- **Framework**: .NET 8.0
- **UI**: WinUI 3 (Windows App SDK)
- **Language**: C# 12
- **MVVM**: CommunityToolkit.Mvvm
- **Charting**: LiveChartsCore
- **Logging**: Serilog

---

## Security Model

AIM implements a centralized database security architecture:

### Authentication & Authorization

**Database-Centric Model**:
- Centralized SQLite database stores all authorized users
- Windows username authentication (no passwords required)
- Network-accessible database enables real-time synchronization

### Role-Based Access Control

**Three Access Levels**:

1. **Basic User (Level 1)**:
   - View and browse assets
   - Search functionality
   - Read-only operations

2. **Admin (Level 2)**:
   - All Basic permissions
   - Modify directory settings
   - Manage users (add/edit/remove)
   - Clear audit logs
   - Change configuration paths

3. **SuperAdmin (Level 3)**:
   - All Admin permissions
   - Full unrestricted access
   - Automatically created during installation

### Security Database

**Location**: Network share (configured during installation)

**Schema**:
- **AuthorizedUsers**: User records with access levels, active status, and audit fields
- **SecuritySettings**: Application-wide security configuration (key-value pairs)
- **SecurityAuditLog**: Complete security event history

### Synchronization

- **30-Second Refresh**: All AIM instances sync users from database automatically
- **Real-Time Updates**: Changes to users propagate within 30 seconds
- **Manual Refresh**: Available via Settings → User Management

### Audit Trail

Every action is logged with:
- Timestamp (UTC)
- User ID (Windows username)
- Action type
- Target path/resource or user
- Detailed operation information
- Success/failure status

Application audit logs: `%LocalAppData%\AIM\Logs\audit_log.json`  
Security audit logs: Stored in SecurityAuditLog table in database

For more details, see the **Security Architecture** section in [ARCHITECTURE.md](ARCHITECTURE.md) and [IMPLEMENTATION-DATABASE-SECURITY.md](IMPLEMENTATION-DATABASE-SECURITY.md).

---

## Configuration

### Application Settings

Settings are stored in: `%LocalAppData%\Microsoft.WinUI.3\AIM\settings.json`

**Hardcoded Paths** (set by installer):
- Default root directory
- Archive path
- Shipped directory
- File scans directory
- Inventory archive directory
- Security database path (network location)

**User-Configurable Options**:
- Theme preference (Light, Dark, High Contrast, Follow System)
- Form template preferences
- Recently accessed paths

**Admin/SuperAdmin Configuration**:
- Admin and SuperAdmin users can change directory paths through Settings → Directory Configuration
- Changes are saved to local settings.json
- Database location is set during installation and cannot be changed (requires reinstall)

### Security Configuration

Security is managed through a centralized SQLite database (not local files):

**Database Location**: Configured during installation (e.g., `\\server\share\...\AIM_Security.db`)

**User Management**:
- Admin and SuperAdmin users manage users through Settings → User Management
- Add, edit, or remove users with appropriate access levels
- Changes sync to all AIM instances within 30 seconds

**No Local Security Files**: AIM no longer uses encrypted local security configuration files. All security is database-driven.

---

## Usage Guide

### File Browsing

1. Select a root directory from the dropdown or browse to a new location
2. Navigate the directory tree in the left pane
3. View file contents in the right pane
4. Use the breadcrumb navigation to move between directories
5. Perform operations: Move, Copy, Delete, Rename

### Search

**Filename Search**:
- Fast, metadata-only search
- Supports wildcards
- Case-insensitive

**Content Search**:
- Full-text search within files
- Slower but comprehensive
- Case-insensitive

### Inventory Management

Requires authorized access (master password or authorized user):
1. Navigate to **Inventory Tools**
2. Select operation:
   - **Directory Archiver**: Archive directory structures
   - **Directory Cloner**: Clone directory layouts
   - **Batch Renamer**: Rename multiple files
   - **Directory Analysis**: Analyze and report on directory contents

### Form Generation

1. Navigate to **Paperwork Forms**
2. Select template type (Ohio, I&M)
3. Fill in form data
4. Preview the generated form
5. Print or save as needed

### Theme Customization

1. Navigate to **Settings**
2. Select theme:
   - **Follow System**: Auto-detect Windows theme
   - **Light**: Force light theme
   - **Dark**: Force dark theme
   - **High Contrast**: Accessibility mode

---

## Development

### Setting Up Development Environment

For detailed setup instructions, see [CONTRIBUTING.md](CONTRIBUTING.md).

**Quick Start**:
1. Install Visual Studio 2022 with WinUI 3 workload
2. Clone the repository
3. Open `AIM.sln`
4. Build and run

### Project Structure

```
AIM/
├── Views/              # XAML pages and controls
├── ViewModels/         # MVVM ViewModels
├── Services/           # Business services
├── Models/             # Data models
├── Converters/         # XAML value converters
├── Messages/           # MVVM messaging
└── Assets/             # Resources and images
```

### Key Design Patterns

- **MVVM**: See [DESIGN_PATTERNS.md](DESIGN_PATTERNS.md#mvvm-pattern)
- **Dependency Injection**: See [DESIGN_PATTERNS.md](DESIGN_PATTERNS.md#dependency-injection)
- **Service Layer**: See [DESIGN_PATTERNS.md](DESIGN_PATTERNS.md#service-layer-pattern)
- **Repository Pattern**: See [DESIGN_PATTERNS.md](DESIGN_PATTERNS.md#repository-pattern)

### Adding New Features

See the **Extension Guide** section in [ARCHITECTURE.md](ARCHITECTURE.md#extension-guide) for step-by-step instructions on:
- Adding new pages
- Creating new services
- Implementing new ViewModels
- Adding audit logging

---

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Code style guidelines
- Commit message conventions
- Pull request process
- Testing requirements
- Security considerations

### Before Contributing

1. Read [CONTRIBUTING.md](CONTRIBUTING.md)
2. Review [ARCHITECTURE.md](ARCHITECTURE.md) to understand the system design
3. Check [DESIGN_PATTERNS.md](DESIGN_PATTERNS.md) for pattern implementations
4. Look for issues labeled `good first issue` or `help wanted`

---

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## Documentation

- **[README-INSTALLER.md](README-INSTALLER.md)** - Complete guide for building and distributing the AIM installer
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture, data flows, service documentation, and extension guide
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Developer guidelines, code standards, and contribution process
- **[DESIGN_PATTERNS.md](DESIGN_PATTERNS.md)** - Design pattern implementations, examples, and anti-patterns

---

## Support

For questions, bug reports, or feature requests:
- Open an [Issue](https://github.com/gwchristen/AIM/issues)
- Check existing [Discussions](https://github.com/gwchristen/AIM/discussions)

---

**Built with ❤️ using WinUI 3 and .NET 8.0**
