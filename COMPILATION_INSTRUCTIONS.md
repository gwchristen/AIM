# Compilation Instructions

## Overview
This document provides step-by-step instructions for compiling the AIM application and installer.

For detailed installer build instructions, see [README-INSTALLER.md](README-INSTALLER.md).

## Prerequisites

### Required Software
- **Windows 10 or Windows 11**
- **Visual Studio 2022** (version 17.8 or later)
  - Workload: **.NET Desktop Development**
  - Workload: **Windows App SDK (WinUI 3)**
- **.NET 8.0 SDK** or later
- **Git** for version control

### Verify Installation

Open PowerShell or Command Prompt and verify:

```bash
# Check .NET SDK version
dotnet --version
# Should show 8.0.0 or later

# Check Git
git --version
```

## Compiling the AIM Application

### 1. Clone the Repository

```bash
git clone https://github.com/gwchristen/AIM.git
cd AIM
```

### 2. Restore Dependencies

```bash
dotnet restore AIM.csproj
```

### 3. Build the Application

**Using .NET CLI:**
```bash
# Debug build
dotnet build AIM.csproj -c Debug

# Release build
dotnet build AIM.csproj -c Release
```

**Using Visual Studio:**
1. Open `AIM.sln` in Visual Studio 2022
2. Select **Build → Build Solution** (or press `Ctrl+Shift+B`)

### 4. Run the Application

**Using .NET CLI:**
```bash
dotnet run --project AIM.csproj
```

**Using Visual Studio:**
1. Set `AIM` as the startup project (if not already)
2. Press `F5` to run in Debug mode
3. Press `Ctrl+F5` to run without debugging

### 5. Locate Build Output

Build artifacts are located in:
- Debug: `bin/Debug/net8.0-windows/`
- Release: `bin/Release/net8.0-windows/`

## Compiling the Installer

The installer is a separate Windows Forms project that packages the AIM application.

### Quick Build (Recommended)

Use the provided PowerShell script:

```powershell
# Build with default settings (Release, win-x64)
.\Build-Installer.ps1

# Build for specific runtime
.\Build-Installer.ps1 -Runtime win-x86
.\Build-Installer.ps1 -Runtime win-arm64

# Build Debug configuration
.\Build-Installer.ps1 -Configuration Debug
```

Output: `bin/AIM-Installer-{runtime}.exe`

### Manual Build Steps

If you prefer to build manually:

1. **Publish AIM Application:**
   ```bash
   dotnet publish AIM.csproj -c Release -r win-x64 --self-contained true -o bin/Publish/win-x64
   ```

2. **Create Resources Directory:**
   ```powershell
   New-Item -ItemType Directory -Path "AIM.Installer/Resources" -Force
   ```

3. **Create ZIP Archive:**
   ```powershell
   Compress-Archive -Path "bin/Publish/win-x64/*" -DestinationPath "AIM.Installer/Resources/AIM-Published.zip" -Force
   ```

4. **Build Installer:**
   ```bash
   dotnet build AIM.Installer/AIM.Installer.csproj -c Release
   ```

5. **Locate Output:**
   `AIM.Installer/bin/Release/net8.0-windows/AIM-Installer.exe`

For complete installer documentation, see [README-INSTALLER.md](README-INSTALLER.md).

## Publishing for Distribution

### Publish Self-Contained Application

```bash
# Windows x64
dotnet publish AIM.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true

# Windows x86
dotnet publish AIM.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true

# Windows ARM64
dotnet publish AIM.csproj -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true
```

Published output includes:
- Complete .NET 8.0 runtime
- All application dependencies
- No .NET installation required on target machine

## Troubleshooting

### Build Errors

**"SDK not found"**
- Install .NET 8.0 SDK from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

**"Windows App SDK not found"**
- Install Windows App SDK workload in Visual Studio Installer

**"NuGet restore failed"**
- Run `dotnet restore` manually
- Clear NuGet cache: `dotnet nuget locals all --clear`

### Runtime Errors

**"Application failed to start"**
- Verify .NET 8.0 runtime is installed (for framework-dependent builds)
- For self-contained builds, check all DLLs are present

**"Database not accessible"**
- Verify network path to security database is accessible
- Check SecurityDatabasePath in settings.json

## Additional Resources

- [README.md](README.md) - Project overview and features
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture and design
- [CONTRIBUTING.md](CONTRIBUTING.md) - Development guidelines
- [README-INSTALLER.md](README-INSTALLER.md) - Detailed installer build guide

## Support

For build issues or questions:
- Check existing [GitHub Issues](https://github.com/gwchristen/AIM/issues)
- Review [CONTRIBUTING.md](CONTRIBUTING.md) for development setup
- Open a new issue if problem persists
