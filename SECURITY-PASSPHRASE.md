# Passphrase-Based Shared Security Configuration

## Overview

This feature allows AIM to automatically connect to a shared encrypted security configuration using a passphrase mechanism. The installer embeds an obfuscated passphrase and writes it into installed settings, enabling clients to decrypt the shared security file without manual user interaction.

## Architecture

### Encryption Modes

AIM now supports two encryption modes for security configurations:

1. **DPAPI Mode** (default for local configs)
   - Uses Windows Data Protection API
   - Machine and user-specific encryption
   - Cannot be shared across users or machines
   - Backward compatible with existing installations

2. **Passphrase Mode** (for shared configs)
   - Uses AES-256-GCM encryption with PBKDF2 key derivation
   - 100,000 iterations of PBKDF2 with SHA-256
   - Random salt per encryption
   - Can be shared across users and machines
   - Requires passphrase for decryption

### Components Modified

1. **Services/EncryptedSettingsService.cs**
   - Added `EncryptionMode` field to `EncryptedSecurityConfig`
   - Modified `SaveSecurityConfigAsync` to accept optional passphrase parameter
   - Modified `LoadSecurityConfigAsync` to accept optional passphrase parameter
   - Added `EncryptWithPassphrase` and `DecryptWithPassphrase` helper methods

2. **Models/AppSettings.cs**
   - Added `Passphrase` property for storing obfuscated passphrase

3. **Services/SecurityService.cs**
   - Modified `InitializeAsync` to deobfuscate and pass passphrase to `LoadSecurityConfigAsync`
   - Added `DeobfuscatePassphrase` helper method

4. **AIM.Installer/InstallerForm.cs**
   - Added obfuscated passphrase constant
   - Added `ObfuscatePassphrase` and `DeobfuscatePassphrase` helper methods
   - Added `WriteInstallerSettings` to write passphrase to settings.json
   - Added `CreateSecurityConfigIni` to create security-config.ini file
   - Modified installation flow to automatically configure passphrase

## Security Considerations

### ⚠️ IMPORTANT SECURITY WARNINGS

1. **Passphrase Obfuscation is NOT Encryption**
   - The passphrase is obfuscated using simple XOR with a static key
   - This only prevents casual discovery in configuration files
   - A determined attacker can extract the passphrase by:
     - Examining the installer binary
     - Reading the settings.json file
     - Debugging the application memory
     - Reverse engineering the obfuscation algorithm

2. **Trade-offs**
   - **Convenience**: Users can connect to shared security automatically
   - **Risk**: Global passphrase embedded in all installer copies
   - **Scope**: If compromised, all installations are affected

3. **Not Recommended For**
   - High-security environments
   - Compliance-regulated industries (HIPAA, PCI-DSS, etc.)
   - Organizations with strict security policies
   - Public-facing deployments

### Recommended Production Alternatives

For production deployments, consider these more secure approaches:

1. **Azure Key Vault Integration**
   - Store passphrase in Azure Key Vault
   - Retrieve using managed identities or service principals
   - Provides audit logging and access control

2. **Domain Certificate Authentication**
   - Use machine or user certificates from domain CA
   - Encrypt shared config with certificate public key
   - Decrypt with private key from certificate store

3. **Hardware Security Modules (HSM)**
   - Store encryption keys in HSM
   - Prevent key extraction
   - Enterprise-grade security

4. **Active Directory Group Policy**
   - Distribute passphrase via Group Policy Preferences
   - Encrypt with AD infrastructure
   - Centralized management

## Usage

### For Administrators

1. **Setting Up Shared Security Config**
   
   **Note**: Deploy-AIM.ps1 has been simplified to only provision directories and no longer handles security configuration.
   
   To create a passphrase-encrypted security.config, use the AIM application directly or create the configuration manually.

2. **Building Installer with Embedded Passphrase**
   - Edit `AIM.Installer/InstallerForm.cs`
   - Update the `ObfuscatedPassphrase` constant with your obfuscated passphrase
   - Use the provided `ObfuscatePassphrase` method to generate the value
   - Build the installer: `.\Build-Installer.ps1`

3. **Distributing Installer**
   - Distribute the built installer to users
   - Users run installer and select shared security path
   - Passphrase is automatically configured

### For End Users

1. **Installation**
   - Run the AIM installer
   - Check "Enable shared security configuration"
   - Select the shared security path (UNC or local path)
   - Complete installation

2. **Automatic Connection**
   - AIM automatically decrypts shared security config on startup
   - No manual passphrase entry required
   - Falls back to local config if shared is unavailable

## Troubleshooting

### "Security configuration is encrypted with a passphrase, but no passphrase was provided"

**Cause**: Shared security config is encrypted with passphrase mode, but settings.json doesn't contain the passphrase.

**Solutions**:
1. Run the installer again to configure passphrase
2. Manually add passphrase to `%LOCALAPPDATA%\AIM\settings.json`:
   ```json
   {
     "Passphrase": "obfuscated_value_here",
     "UseSharedConfig": true,
     "SharedSecurityConfigPath": "\\\\server\\share\\path"
   }
   ```

### "Failed to decrypt security configuration. The passphrase may be incorrect."

**Cause**: The passphrase in settings.json doesn't match the one used to encrypt the shared config.

**Solutions**:
1. Verify the shared config was encrypted with the correct passphrase
2. Re-encrypt the shared config with the correct passphrase
3. Update all installers with the correct obfuscated passphrase

## Implementation Details

### Passphrase Obfuscation Algorithm

```csharp
// XOR key used for obfuscation (NOT secure encryption!)
byte[] xorKey = { 0xA5, 0x3C, 0x7E, 0x91, 0x42, 0xF8, 0x6D, 0x2B };

// To obfuscate:
byte[] data = Encoding.UTF8.GetBytes(passphrase);
for (int i = 0; i < data.Length; i++)
    data[i] ^= xorKey[i % xorKey.Length];
string obfuscated = Convert.ToBase64String(data);

// To deobfuscate (reverse is identical):
byte[] data = Convert.FromBase64String(obfuscated);
for (int i = 0; i < data.Length; i++)
    data[i] ^= xorKey[i % xorKey.Length];
string passphrase = Encoding.UTF8.GetString(data);
```

### AES-GCM Encryption Parameters

- **Algorithm**: AES-256-GCM
- **Key Derivation**: PBKDF2 (RFC 2898)
- **Hash Function**: SHA-256
- **Iterations**: 100,000
- **Salt Size**: 16 bytes (128 bits)
- **Nonce Size**: 12 bytes (96 bits)
- **Tag Size**: 16 bytes (128 bits)

### Encrypted Data Format

Passphrase-encrypted configs store data as a JSON object:

```json
{
  "salt": "base64_encoded_salt",
  "nonce": "base64_encoded_nonce",
  "tag": "base64_encoded_auth_tag",
  "data": "base64_encoded_ciphertext"
}
```

## Version History

- **Version 1.0** (Current)
  - Initial implementation
  - DPAPI and Passphrase modes
  - Simple XOR obfuscation
  - Installer integration

## Future Enhancements

Potential improvements for future versions:

1. **Certificate-Based Encryption**
   - Support for X.509 certificates
   - Public/private key cryptography
   - No shared secrets

2. **Key Rotation**
   - Ability to change passphrase without re-deploying
   - Version management for multiple passphrases

3. **Multi-Factor Authentication**
   - Combine passphrase with hardware token
   - TOTP/HOTP support

4. **Audit Logging**
   - Log all decryption attempts
   - Failed authentication tracking
   - Compliance reporting

## Contact

For security concerns or questions about this implementation, please contact the development team.
