# AIM Installer - Passphrase Configuration Guide

## Overview

The AIM installer now supports automatic configuration of shared security using an embedded passphrase. This allows clients to connect to a centralized security configuration without manual passphrase entry.

## Quick Start

### For Administrators

1. **Generate an Obfuscated Passphrase**
   ```csharp
   // Use PassphraseObfuscationExample.cs utility
   // Update the plainPassphrase variable
   // Run to get obfuscated value
   ```

2. **Update Installer with Passphrase**
   - Open `AIM.Installer/InstallerForm.cs`
   - Locate the `ObfuscatedPassphrase` constant
   - Replace with your generated obfuscated value
   ```csharp
   private const string ObfuscatedPassphrase = "YOUR_OBFUSCATED_VALUE_HERE";
   ```

3. **Build the Installer**
   ```powershell
   .\Build-Installer.ps1
   ```

4. **Distribute to Users**
   - Share the built installer executable
   - Users run installer and enable shared security
   - Passphrase is automatically configured

### For End Users

1. **Run the Installer**
   - Double-click the AIM installer executable
   - Follow the installation wizard

2. **Configure Shared Security**
   - On the "Shared Security" screen:
     - ☑ Check "Enable shared security configuration"
     - Select or enter the shared security path
   - Click "Install"

3. **Automatic Connection**
   - AIM will automatically decrypt shared security config on first launch
   - No manual passphrase entry required

## Architecture

### Installation Process

```
Installer Workflow:
1. Extract AIM application files
2. If shared security enabled:
   a. Write settings.json to %LOCALAPPDATA%\AIM\
   b. Include obfuscated passphrase in settings
   c. Set UseSharedConfig=true
   d. Set SharedSecurityConfigPath
   e. Create security-config.ini in install directory
3. Create shortcuts (desktop/start menu)
4. Optional: Run Deploy-AIM.ps1 script
```

### Files Created

1. **%LOCALAPPDATA%\AIM\settings.json**
   ```json
   {
     "UseSharedConfig": true,
     "SharedSecurityConfigPath": "\\\\server\\share\\AIM_Security\\security.config",
     "Passphrase": "obfuscated_value_here"
   }
   ```

2. **{InstallPath}\security-config.ini**
   ```ini
   # AIM Shared Security Configuration
   SharedSecurityPath=\\server\share\AIM_Security\security.config
   ```

### Runtime Behavior

```
Application Startup:
1. Load settings.json from %LOCALAPPDATA%\AIM\
2. SecurityService.InitializeAsync():
   a. Deobfuscate passphrase from settings
   b. Try to load shared security config with passphrase
   c. On success: Cache locally for offline access
   d. On failure: Fall back to local cached config
   e. On no config: Enter first-time setup mode
```

## Configuration Options

### Option 1: Embedded Passphrase (Automatic)

**Pros:**
- No user interaction required
- Seamless deployment
- Works offline (after first connection)

**Cons:**
- Passphrase embedded in installer binary
- All installations share same passphrase
- Lower security

**Use Cases:**
- Internal corporate deployments
- Trusted network environments
- Convenience over security requirements

### Option 2: Manual Passphrase Entry

**Pros:**
- Passphrase not stored in installer
- Can be different per user/installation
- Higher security

**Cons:**
- Requires user training
- Manual setup on each machine
- User must remember/manage passphrase

**Use Cases:**
- High-security environments
- Compliance-regulated industries
- External deployments

### Option 3: No Shared Security (Local Only)

**Pros:**
- Highest security (DPAPI)
- No network dependencies
- User/machine specific

**Cons:**
- No centralized management
- Manual sync required for multi-user
- Each user has own config

**Use Cases:**
- Single-user installations
- Disconnected environments
- Maximum security requirements

## Security Considerations

### ⚠️ Critical Security Information

The embedded passphrase approach has important security implications:

1. **Obfuscation ≠ Encryption**
   - XOR with static key is easily reversible
   - Provides NO cryptographic protection
   - Only prevents casual observation

2. **Attack Surface**
   - Installer binary can be disassembled
   - settings.json is plain text (Base64)
   - Memory can be dumped while running
   - Network traffic can be monitored

3. **Compromise Impact**
   - If passphrase is extracted, ALL installations are affected
   - Shared security config becomes accessible to anyone
   - Requires new passphrase and redistribution

### Security Best Practices

1. **For Development/Testing**
   - Embedded passphrase is acceptable
   - Use non-production data
   - Rotate passphrases regularly

2. **For Internal Corporate Use**
   - Acceptable with risk acknowledgment
   - Document the security trade-off
   - Implement network segmentation
   - Monitor access logs

3. **For Production/External Use**
   - **NOT RECOMMENDED** - Use alternatives below

### Recommended Alternatives for Production

#### Azure Key Vault Integration
```csharp
// Retrieve passphrase from Key Vault at runtime
var credential = new DefaultAzureCredential();
var client = new SecretClient(vaultUri, credential);
var secret = await client.GetSecretAsync("aim-passphrase");
string passphrase = secret.Value.Value;
```

#### Certificate-Based Encryption
```csharp
// Encrypt with certificate public key
// Decrypt with private key from cert store
var cert = GetCertificateFromStore("CN=AIM-Security");
var encrypted = cert.PublicKey.Encrypt(data);
```

#### Active Directory Group Policy
```powershell
# Distribute via GPP (encrypted)
New-GPPPassphrase -Name "AIM-Passphrase" -Value $securePassphrase
```

## Troubleshooting

### Problem: "Security configuration is encrypted with a passphrase, but no passphrase was provided"

**Cause:** settings.json missing or doesn't contain passphrase

**Solutions:**
1. Re-run the installer
2. Manually add passphrase to settings.json:
   ```json
   {
     "Passphrase": "obfuscated_value",
     "UseSharedConfig": true,
     "SharedSecurityConfigPath": "\\\\server\\share\\path"
   }
   ```

### Problem: "Failed to decrypt security configuration. The passphrase may be incorrect."

**Cause:** Passphrase mismatch between installer and shared config

**Solutions:**
1. Verify shared config encryption passphrase
2. Re-encrypt shared config with correct passphrase
3. Rebuild installer with correct obfuscated passphrase
4. Redistribute installer to users

### Problem: Installer doesn't write settings.json

**Cause:** Shared security not enabled during installation

**Solutions:**
1. Re-run installer
2. Check "Enable shared security configuration"
3. Select valid shared security path
4. Complete installation

### Problem: Application can't access shared security path

**Cause:** Network path not accessible, permissions issue

**Solutions:**
1. Verify network path is accessible
2. Check user has read permissions
3. Test with UNC path: `\\server\share\path`
4. Falls back to local cached config automatically

## Development Guide

### Building with Custom Passphrase

1. **Generate Obfuscated Value**
   ```csharp
   // In PassphraseObfuscationExample.cs
   string plain = "YourSecurePassphrase123!";
   string obfuscated = ObfuscatePassphrase(plain);
   Console.WriteLine(obfuscated);
   ```

2. **Update InstallerForm.cs**
   ```csharp
   private const string ObfuscatedPassphrase = "generated_value_here";
   ```

3. **Build Installer**
   ```powershell
   .\Build-Installer.ps1
   ```

4. **Test Installation**
   - Run installer on test machine
   - Enable shared security
   - Verify settings.json created
   - Launch AIM and verify connection

### Creating Shared Security Config

Use Deploy-AIM.ps1 with passphrase:

```powershell
.\Deploy-AIM.ps1 `
    -AIMInstallPath "C:\Program Files\AIM" `
    -SharedSecurityPath "\\server\share\AIM_Security\security.config" `
    -Passphrase "YourSecurePassphrase123!" `
    -DefaultRootDirectory "C:\AIM\Data" `
    -ArchivePath "C:\AIM\Archive" `
    -ShippedDirectory "C:\AIM\Shipped" `
    -FileScansDirectory "C:\AIM\FileScans" `
    -InventoryArchiveDirectory "C:\AIM\InventoryArchive"
```

This creates a passphrase-encrypted security.config that can be shared.

## Files Modified

### Core Application
- `Services/EncryptedSettingsService.cs` - Added passphrase encryption support
- `Services/IEncryptedSettingsService.cs` - Updated interface
- `Services/SecurityService.cs` - Added passphrase deobfuscation
- `Models/AppSettings.cs` - Added Passphrase property

### Installer
- `AIM.Installer/InstallerForm.cs` - Added passphrase functionality
- `AIM.Installer/PassphraseObfuscationExample.cs` - Utility for developers

### Scripts
- `Deploy-AIM.ps1` - Added passphrase parameter

### Documentation
- `SECURITY-PASSPHRASE.md` - Comprehensive security documentation
- `README-INSTALLER-PASSPHRASE.md` - This file

## Version History

- **v1.0** - Initial passphrase-based shared security implementation

## Support

For questions or issues:
1. Check SECURITY-PASSPHRASE.md for detailed information
2. Review troubleshooting section above
3. Contact development team

## License

Same as AIM application license.
