# AIM Installer Testing Guide

This document provides a comprehensive testing checklist for validating the AIM installer before distribution.

## Prerequisites for Testing

- **Windows 10 or Windows 11** (installer only runs on Windows)
- **Visual Studio 2022** (optional, for debugging)
- **.NET SDK 8.0 or later**
- **PowerShell 5.1 or later**

## Build Validation

### 1. Build Script Execution

Test the build script with various configurations:

```powershell
# Test default build (Release, win-x64)
.\Build-Installer.ps1

# Verify output
Test-Path "bin\AIM-Installer-win-x64.exe"

# Test win-x86 build
.\Build-Installer.ps1 -Runtime win-x86
Test-Path "bin\AIM-Installer-win-x86.exe"

# Test Debug configuration
.\Build-Installer.ps1 -Configuration Debug -Runtime win-x64
```

**Expected Results:**
- ✅ Build completes without errors
- ✅ Installer EXE is created in `bin/` directory
- ✅ File size is approximately 100-150 MB
- ✅ No PowerShell errors during build

### 2. Build Artifacts Validation

Check that all necessary components are embedded:

```powershell
# Check if resources were created
Test-Path "AIM.Installer\Resources\AIM-Published.zip"
Test-Path "AIM.Installer\Resources\Deploy-AIM.ps1"

# Verify ZIP contents
Expand-Archive -Path "AIM.Installer\Resources\AIM-Published.zip" -DestinationPath "temp-verify" -Force
Test-Path "temp-verify\AIM.exe"
Remove-Item "temp-verify" -Recurse -Force
```

**Expected Results:**
- ✅ Resources directory contains ZIP and PS1
- ✅ ZIP contains AIM.exe and dependencies
- ✅ Deploy-AIM.ps1 is included

### 3. Single EXE Verification

Verify the installer is a single executable:

```powershell
# Check installer directory
Get-ChildItem "AIM.Installer\bin\Release\net8.0-windows" -File | Where-Object { $_.Extension -eq ".dll" }
```

**Expected Results:**
- ✅ No standalone DLL files (Costura should embed all dependencies)
- ✅ Only AIM-Installer.exe and metadata files

## Installation Testing

### 4. Basic Installation Flow

Test the complete installation wizard:

1. **Launch Installer**
   - Double-click `AIM-Installer-win-x64.exe`
   - ✅ Installer window opens without errors
   - ✅ Welcome screen displays correctly

2. **Welcome Screen**
   - ✅ Title and description are correct
   - ✅ "Next" button is enabled
   - ✅ "Cancel" button works

3. **Installation Path Screen**
   - ✅ Default path is `C:\Program Files\AIM`
   - ✅ Browse button opens folder dialog
   - ✅ Can select custom directory
   - ✅ Desktop shortcut checkbox is checked by default
   - ✅ Start Menu shortcut checkbox is checked by default

4. **Shared Security Screen**
   - ✅ Enable checkbox toggles controls correctly
   - ✅ Browse button is disabled when unchecked
   - ✅ Browse button works when enabled

5. **Installation Progress**
   - ✅ Progress log shows real-time updates
   - ✅ No error messages in log
   - ✅ All files extracted successfully

6. **Completion Screen**
   - ✅ Success message displayed
   - ✅ "Launch AIM after installation" checkbox visible
   - ✅ "Finish" button closes installer

### 5. Installation Verification

After successful installation:

```powershell
# Verify installation directory
$installPath = "C:\Program Files\AIM"
Test-Path "$installPath\AIM.exe"
Test-Path "$installPath\Deploy-AIM.ps1"

# Verify shortcuts
$desktopShortcut = "$env:USERPROFILE\Desktop\AIM.lnk"
$startMenuShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\AIM\AIM.lnk"
Test-Path $desktopShortcut
Test-Path $startMenuShortcut
```

**Expected Results:**
- ✅ AIM.exe exists in installation directory
- ✅ Deploy-AIM.ps1 exists in installation directory
- ✅ All required DLLs and dependencies present
- ✅ Desktop shortcut created (if selected)
- ✅ Start Menu shortcut created (if selected)

### 6. Shortcut Functionality

Test the created shortcuts:

1. **Desktop Shortcut**
   - Double-click Desktop shortcut
   - ✅ AIM launches successfully
   - ✅ No error dialogs

2. **Start Menu Shortcut**
   - Launch from Start Menu
   - ✅ AIM launches successfully
   - ✅ Appears in Start Menu under "AIM" folder

### 7. Application Launch Test

Verify AIM runs correctly after installation:

```powershell
# Launch AIM directly
& "C:\Program Files\AIM\AIM.exe"
```

**Expected Results:**
- ✅ AIM window opens
- ✅ No missing DLL errors
- ✅ Application is functional

## Advanced Testing

### 8. Custom Installation Path

Test installation to a custom directory:

1. Run installer
2. Change path to `C:\CustomLocation\AIM`
3. Complete installation

**Expected Results:**
- ✅ Files extracted to custom location
- ✅ Shortcuts point to correct path
- ✅ AIM launches from custom location

### 9. Database Security Configuration

Test the database-based security feature:

1. Run installer
2. Verify security database path is configured in settings.json
3. Complete installation
4. Launch AIM
5. Verify database connection

**Expected Results:**
- ✅ SecurityDatabasePath configured in settings.json
- ✅ Security database initialized
- ✅ SuperAdmin account created
- ✅ No errors in installation log

### 10. Installation Without Shortcuts

Test installation without creating shortcuts:

1. Run installer
2. Uncheck both shortcut options
3. Complete installation

**Expected Results:**
- ✅ Installation completes successfully
- ✅ No shortcuts created on Desktop
- ✅ No shortcuts in Start Menu
- ✅ AIM still functional from install directory

### 11. Launch After Install

Test the "Launch AIM after installation" feature:

1. Run installer
2. Complete installation
3. Ensure "Launch AIM" checkbox is checked
4. Click "Finish"

**Expected Results:**
- ✅ Installer closes
- ✅ AIM launches automatically
- ✅ AIM is fully functional

### 12. Reinstallation Test

Test installing over an existing installation:

1. Install AIM to default location
2. Run installer again
3. Use same installation path
4. Complete installation

**Expected Results:**
- ✅ Files are overwritten
- ✅ No error messages
- ✅ Installation completes successfully
- ✅ AIM still functional

### 13. Uninstallation Verification

After installation, verify manual uninstallation:

```powershell
# Remove installation directory
Remove-Item "C:\Program Files\AIM" -Recurse -Force

# Remove shortcuts
Remove-Item "$env:USERPROFILE\Desktop\AIM.lnk" -Force
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\AIM" -Recurse -Force
```

**Expected Results:**
- ✅ All files removed successfully
- ✅ No orphaned files or directories

## Error Handling Testing

### 14. Permission Errors

Test installation with insufficient permissions:

1. Run installer **without** administrator privileges
2. Select `C:\Program Files\AIM` (requires admin)
3. Attempt installation

**Expected Results:**
- ✅ Error message about insufficient permissions
- ✅ Installer handles error gracefully
- ✅ Or: prompts for elevation (future enhancement)

### 15. Disk Space Errors

Test installation with insufficient disk space (if possible):

1. Fill up target drive
2. Run installer
3. Attempt installation

**Expected Results:**
- ✅ Clear error message about disk space
- ✅ Installation does not corrupt existing files

### 16. Cancellation Test

Test canceling installation at various stages:

1. **At Welcome Screen**: Click "Cancel"
   - ✅ Installer closes immediately

2. **At Path Selection**: Click "Cancel"
   - ✅ Confirmation dialog appears
   - ✅ Installer closes on confirmation

3. **During Installation**: Cannot cancel (buttons disabled)
   - ✅ Buttons correctly disabled during extraction

## Security Testing

### 17. Antivirus Scan

Scan the installer with antivirus software:

```powershell
# Windows Defender scan (example)
Start-MpScan -ScanPath "bin\AIM-Installer-win-x64.exe" -ScanType CustomScan
```

**Expected Results:**
- ✅ No malware detected
- ✅ No suspicious behavior flagged
- ✅ (Note: May show false positive due to self-extraction)

### 18. Code Signing (Optional)

If code-signed, verify signature:

```powershell
Get-AuthenticodeSignature "bin\AIM-Installer-win-x64.exe"
```

**Expected Results:**
- ✅ Signature is valid
- ✅ Certificate information correct
- ✅ No signature errors

### 19. SmartScreen Test

Test Windows SmartScreen behavior:

1. Download installer to new machine
2. Attempt to run

**Expected Results:**
- ✅ Without signing: SmartScreen warning (expected)
- ✅ With signing: Runs without warning
- ✅ User can bypass warning via "More info"

## Performance Testing

### 20. Installation Speed

Measure installation time:

1. Start timer when clicking "Install"
2. Stop when "Installation Complete" appears

**Expected Results:**
- ✅ Installation completes in under 2 minutes on typical hardware
- ✅ Progress log updates smoothly
- ✅ No UI freezing

### 21. Installer Size

Verify installer size is reasonable:

```powershell
(Get-Item "bin\AIM-Installer-win-x64.exe").Length / 1MB
```

**Expected Results:**
- ✅ Size between 100-150 MB
- ✅ Not excessively large (>200 MB)

## Compatibility Testing

### 22. Windows Version Compatibility

Test on different Windows versions:

- ✅ **Windows 10 (21H2 or later)**
- ✅ **Windows 11**
- ❌ Windows 8.1 (not supported - requires Windows 10)

### 23. Architecture Compatibility

Test different builds:

- ✅ **win-x64** on 64-bit Windows
- ✅ **win-x86** on 32-bit Windows (if applicable)
- ✅ **win-arm64** on ARM64 Windows (if available)

## Documentation Testing

### 24. README Accuracy

Verify README-INSTALLER.md instructions:

1. Follow build instructions exactly
2. Verify all commands work
3. Check for typos or outdated information

**Expected Results:**
- ✅ All instructions accurate
- ✅ No broken links
- ✅ Examples work as documented

## Final Checklist

Before releasing the installer:

- [ ] All build tests pass
- [ ] Installation flow works correctly
- [ ] Shortcuts created and functional
- [ ] AIM launches successfully
- [ ] No security warnings (or documented)
- [ ] Performance is acceptable
- [ ] Documentation is accurate
- [ ] Testing completed on Windows 10 and 11
- [ ] Known issues documented
- [ ] Release notes prepared

## Reporting Issues

If you find issues during testing:

1. **Document the issue**:
   - Steps to reproduce
   - Expected vs. actual behavior
   - Error messages or logs
   - Windows version and build number

2. **Check logs**:
   - Installer progress log (in UI)
   - Windows Event Viewer
   - AIM application logs (if AIM launches)

3. **Report the issue**:
   - Open GitHub issue
   - Include all documentation
   - Tag as "installer" and "bug"

## Testing Matrix

| Test Case | Windows 10 | Windows 11 | win-x64 | win-x86 | Notes |
|-----------|------------|------------|---------|---------|-------|
| Basic Install | [ ] | [ ] | [ ] | [ ] | Default settings |
| Custom Path | [ ] | [ ] | [ ] | [ ] | Non-default directory |
| Shared Security | [ ] | [ ] | [ ] | [ ] | Optional feature |
| No Shortcuts | [ ] | [ ] | [ ] | [ ] | All unchecked |
| Reinstall | [ ] | [ ] | [ ] | [ ] | Over existing |
| Launch After | [ ] | [ ] | [ ] | [ ] | Auto-launch |

## Automation Considerations

For future automation of these tests:

```powershell
# Example: Automated installation test
$installerPath = "bin\AIM-Installer-win-x64.exe"

# Would need silent install support (future enhancement)
# Start-Process $installerPath -ArgumentList "/S", "/D=C:\Test\AIM" -Wait

# Verify installation
$testPath = "C:\Test\AIM"
if (Test-Path "$testPath\AIM.exe") {
    Write-Host "✅ Installation successful"
} else {
    Write-Host "❌ Installation failed"
}
```

---

**Note**: This is a comprehensive testing guide. Not all tests need to be run for every build, but all should be validated before major releases.
