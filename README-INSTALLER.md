# AIM Installer - Build Instructions

This document provides detailed instructions for building the AIM self-extracting installer.

## Overview

The AIM Installer is a self-contained Windows Forms application that packages the entire AIM application into a single portable EXE file. It provides a streamlined installation wizard with the following features:

- **Single EXE Distribution**: No external dependencies or DLLs required
- **User-Friendly GUI**: Windows Forms-based wizard interface with three simple steps
- **Configurable Installation**: User selects installation directory and shortcut options
- **Shortcut Creation**: Automatically creates Start Menu and Desktop shortcuts
- **Hardcoded Configuration**: All directory paths are pre-configured in the installer
- **Launch After Install**: Option to launch AIM immediately after installation

## Prerequisites

### Required Software

1. **Windows Operating System**: Windows 10 or Windows 11
2. **.NET SDK 8.0 or later**: [Download .NET SDK](https://dotnet.microsoft.com/download)
3. **Visual Studio 2022** (Optional but recommended for development):
   - Workload: ".NET desktop development"
   - Individual component: "Windows Forms"

### Verify Prerequisites

Open a PowerShell or Command Prompt window and run:

```powershell
dotnet --version
```

You should see version 8.0.0 or later.

## Project Structure

```
AIM/
├── AIM.csproj                      # Main AIM application project
├── AIM.Installer/                  # Installer project directory
│   ├── AIM.Installer.csproj        # Installer project file
│   ├── Program.cs                  # Entry point
│   ├── InstallerForm.cs            # Main installer UI
│   ├── FodyWeavers.xml             # Costura.Fody configuration
│   └── Resources/                  # Embedded resources (auto-generated)
│       └── AIM-Published.zip       # Published AIM application (build-time)
├── Build-Installer.ps1             # Build automation script
└── README-INSTALLER.md             # This file
```

## Building the Installer

### Option 1: Using the Build Script (Recommended)

The easiest way to build the installer is to use the provided PowerShell script:

```powershell
# Build with default settings (Release, win-x64)
.\Build-Installer.ps1

# Build for a specific runtime
.\Build-Installer.ps1 -Runtime win-x86

# Build Debug configuration
.\Build-Installer.ps1 -Configuration Debug

# Skip cleaning previous builds
.\Build-Installer.ps1 -SkipClean
```

#### Build Script Parameters

- **`-Configuration`**: Build configuration (`Debug` or `Release`). Default: `Release`
- **`-Runtime`**: Target runtime identifier (`win-x64`, `win-x86`, `win-arm64`). Default: `win-x64`
- **`-SkipClean`**: Skip cleaning previous builds (faster incremental builds)

#### Build Script Steps

The build script performs the following steps automatically:

1. **Validates Prerequisites**: Checks for required tools and files
2. **Cleans Previous Builds**: Removes old build artifacts (unless `-SkipClean` is used)
3. **Creates Resources Directory**: Prepares the embedded resources folder
4. **Publishes AIM Application**: 
   - Runs `dotnet publish` with self-contained settings
   - Enables ReadyToRun compilation for better startup performance
   - Enables trimming to reduce size
5. **Creates ZIP Archive**: Packages the published application
6. **Builds Installer**: Compiles the installer project
7. **Finalizes Output**: Copies the installer to `bin/AIM-Installer-{runtime}.exe`

#### Build Output

After successful build, you'll find the installer at:

```
bin/AIM-Installer-win-x64.exe    (for win-x64)
bin/AIM-Installer-win-x86.exe    (for win-x86)
bin/AIM-Installer-win-arm64.exe  (for win-arm64)
```

### Option 2: Manual Build Steps

If you prefer to build manually or need to customize the build process:

#### Step 1: Publish AIM Application

```powershell
dotnet publish AIM.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o bin/Publish/win-x64 `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=true `
    -p:PublishReadyToRun=true
```

#### Step 2: Create Resources Directory

```powershell
New-Item -ItemType Directory -Path "AIM.Installer/Resources" -Force
```

#### Step 3: Create ZIP Archive

```powershell
Compress-Archive -Path "bin/Publish/win-x64/*" `
    -DestinationPath "AIM.Installer/Resources/AIM-Published.zip" `
    -CompressionLevel Optimal `
    -Force
```

#### Step 4: Build Installer

```powershell
dotnet build AIM.Installer/AIM.Installer.csproj -c Release
```

#### Step 5: Locate Output

The installer will be at:
```
AIM.Installer/bin/Release/net8.0-windows/AIM-Installer.exe
```

## How the Installer Works

### Build-Time Process

1. **Resource Embedding**: The build script embeds the AIM application into the installer:
   - `AIM-Published.zip`: Complete self-contained AIM application

2. **Costura.Fody Integration**: Merges all DLL dependencies into the single EXE
   - No external DLLs required
   - Simplified distribution
   - Automatic assembly loading at runtime

### Runtime Process

When the user runs `AIM-Installer.exe`, they experience a streamlined 3-screen wizard:

1. **Welcome Screen**: 
   - Displays welcome message and AIM feature overview
   - Explains the simplified installation process

2. **Installation Path Selection**: 
   - Default: `C:\Program Files\AIM`
   - User can browse to select custom location
   - Options for Desktop and Start Menu shortcuts
   - No security configuration required (handled automatically)

3. **Installation Progress & Completion**:
   - Extracts embedded ZIP to installation directory
   - Creates security database at hardcoded network location
   - Initializes database schema (AuthorizedUsers, SecuritySettings, SecurityAuditLog)
   - Seeds current Windows user as SuperAdmin (AccessLevel = 3)
   - Writes settings.json with hardcoded directory paths to %LOCALAPPDATA%\AIM\
   - Creates shortcuts if selected
   - Shows real-time progress log
   - Option to launch AIM immediately (checked by default)
   - Displays success message

### Hardcoded Configuration

The installer automatically configures AIM with the following hardcoded paths (written to settings.json):

- **DefaultRootDirectory**: `\\oh1cam01\cml\Internal\LAB STOCK\LAB STOCK`
- **ArchivePath**: `\\oh1cam01\cml\Internal\LAB STOCK\Archive`
- **ShippedDirectory**: `\\oh1cam01\cml\Internal\LAB STOCK\Orders shipped`
- **FileScansDirectory**: `C:\Tfile`
- **InventoryArchiveDirectory**: `\\oh1cam01\cml\Internal\LAB STOCK\Physical Inventory Archive`
- **SecurityDatabasePath**: `\\oh1cam01\cml\Internal\LAB STOCK\Important Inventory Related Documents\AIM\AIM_Security.db`

These paths are constants in `InstallerForm.cs` and can be modified before building the installer if different paths are needed.

**Security Initialization:**

The installer also handles security setup automatically:

1. **Database Creation**: Creates `AIM_Security.db` at the SecurityDatabasePath
2. **Schema Initialization**: Creates AuthorizedUsers, SecuritySettings, and SecurityAuditLog tables
3. **SuperAdmin Seeding**: Adds the current installer user as SuperAdmin (AccessLevel = 3)
4. **No Password Required**: Database-centric model eliminates the need for master passwords or passphrases

**Post-Installation Configuration:**

- Admin and SuperAdmin users can modify directory paths through Settings → Directory Configuration
- All configuration changes are saved to local settings.json
- Security database location cannot be changed after installation (requires reinstall)

### Technology Stack

- **Framework**: .NET 8.0 Windows Forms
- **Language**: C# 12
- **IL Weaving**: Costura.Fody (merges dependencies)
- **Compression**: System.IO.Compression (ZIP extraction)

## Customization

### Changing Hardcoded Directory Paths

Edit `InstallerForm.cs` and modify the constants at the top of the class:

```csharp
private const string DefaultRootDirectory = @"\\your-server\your-share\path";
private const string ArchivePath = @"\\your-server\your-share\archive";
// ... etc
```

Then rebuild the installer.

### Changing Default Installation Path

Edit `InstallerForm.cs`:

```csharp
private string installPath = @"C:\Your\Custom\Path";
```

### Modifying UI Appearance

The installer uses standard Windows Forms controls. You can customize:

- Colors in `InstallerForm.cs` (e.g., `topPanel.BackColor`)
- Fonts (e.g., `titleLabel.Font`)
- Control positions and sizes
- Welcome message text

### Adding Custom Installation Steps

Add logic in the `PerformInstallation()` method in `InstallerForm.cs`:

```csharp
private void PerformInstallation()
{
    // ... existing code ...
    
    // Your custom step
    LogMessage("Performing custom step...");
    YourCustomMethod();
    
    // ... remaining code ...
}
```

## Troubleshooting

### Build Issues

#### "AIM project not found"
- Ensure you're running the script from the AIM repository root
- Verify `AIM.csproj` exists in the same directory

#### "dotnet CLI not found"
- Install the .NET SDK 8.0 or later
- Add the .NET SDK to your PATH environment variable

#### "Publish failed"
- Check that you have disk space available
- Verify you have write permissions to the output directories
- Review the detailed error messages in the script output

### Installer Issues

#### "Embedded ZIP not found"
- The installer was built without the embedded resources
- Re-run `Build-Installer.ps1` to ensure resources are embedded

#### "Could not extract files"
- Ensure the user has write permissions to the installation directory
- Try running the installer as Administrator
- Check available disk space

#### "Shortcut creation failed"
- Non-critical warning; installation continues
- May occur if COM automation is restricted
- Shortcuts can be created manually

## Distribution

The final `AIM-Installer-{runtime}.exe` is a completely self-contained executable:

- **No installation required**: Just run the EXE
- **No .NET runtime required**: Self-contained includes the runtime
- **No external dependencies**: All DLLs are embedded
- **Single file**: Easy to distribute via email, USB, or download

### File Size

Typical installer size (varies by runtime and configuration):
- **win-x64 Release**: ~100-150 MB
- **win-x86 Release**: ~90-130 MB
- **win-arm64 Release**: ~100-150 MB

The size includes:
- Complete .NET 8.0 runtime
- AIM application and all dependencies
- Windows App SDK runtime
- All embedded resources

## Security Considerations

### Code Signing (Recommended)

For production distribution, sign the installer with a code signing certificate:

```powershell
# Using SignTool from Windows SDK
signtool sign /f "certificate.pfx" /p "password" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "AIM-Installer.exe"
```

Benefits of code signing:
- Users trust the installer
- Reduces Windows SmartScreen warnings
- Proves authenticity and integrity

### Antivirus False Positives

Self-extracting installers may trigger antivirus warnings:
- **Solution**: Submit to antivirus vendors for whitelisting
- **Solution**: Use code signing to reduce false positives
- **Solution**: Distribute via trusted channels

## Advanced Topics

### Multi-Runtime Distribution

Build installers for all supported platforms:

```powershell
# Build all runtimes
.\Build-Installer.ps1 -Runtime win-x64
.\Build-Installer.ps1 -Runtime win-x86
.\Build-Installer.ps1 -Runtime win-arm64
```

### Automated CI/CD Integration

Example GitHub Actions workflow:

```yaml
- name: Build Installers
  run: |
    .\Build-Installer.ps1 -Runtime win-x64 -Configuration Release
    .\Build-Installer.ps1 -Runtime win-x86 -Configuration Release
    .\Build-Installer.ps1 -Runtime win-arm64 -Configuration Release

- name: Upload Artifacts
  uses: actions/upload-artifact@v3
  with:
    name: installers
    path: bin/AIM-Installer-*.exe
```

### Silent Installation

Currently, the installer requires GUI interaction. For future silent installation support, consider adding command-line arguments:

```csharp
// Future enhancement in Program.cs
if (args.Length > 0 && args[0] == "/silent")
{
    // Perform silent installation
    SilentInstaller.Run(args);
}
else
{
    Application.Run(new InstallerForm());
}
```

## Support

For issues or questions:
- Open an issue on the GitHub repository
- Review existing issues for similar problems
- Check the main AIM README for general application support

## License

The AIM Installer is part of the AIM project and follows the same license (MIT License).

---

**Built with ❤️ using .NET 8.0 and Windows Forms**
