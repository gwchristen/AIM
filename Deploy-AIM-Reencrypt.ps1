<#
.SYNOPSIS
  Re-encrypt an existing AIM shared security config using a shared passphrase (AES-GCM) and write it to the shared location.

.DESCRIPTION
  This script creates a new passphrase-encrypted security.config file (JSON) compatible with the EncryptedSettingsService
  passphrase format (payload contains salt/iv/tag/data). Use this to re-encrypt an existing shared config so clients
  that have the shared passphrase can decrypt it automatically.

  - It does NOT attempt to DPAPI-decrypt an existing DPAPI file. Instead you must supply the master password (plaintext)
    and authorized users (CSV or repeated -AuthorizedUser arguments), and the script will create a new passphrase-encrypted file.
  - Run this on the admin machine where you know the cleartext master password and authorized user list.
  - The script will back up any existing security.config before overwriting.

PARAMETERS
  -SharedSecurityPath   : UNC or local folder that will hold security.config (required)
  -Passphrase           : Passphrase to use for AES-GCM encryption (will prompt securely if not provided)
  -MasterPassword       : The cleartext master password currently in use (will prompt securely if not provided)
  -AuthorizedUsers      : Comma-separated list of authorized users OR repeated parameter usage
  -AIMInstallPath       : Optional local AIM install path to also write a local security-config.ini pointing to the shared path
  -Backup               : Switch to keep a timestamped backup of existing security.config (default behavior is to back up)

EXAMPLE
  PS> .\Deploy-AIM-Reencrypt.ps1 -SharedSecurityPath "\\fileserver\AIM_Security" -Passphrase (Read-Host -AsSecureString | ConvertFrom-SecureString -AsPlainText) `
        -MasterPassword (Read-Host "Master password" -AsSecureString | ConvertFrom-SecureString -AsPlainText) -AuthorizedUsers "DOMAIN\User1,DOMAIN\User2"

NOTE
  - The script implements PBKDF2 (100000 iterations, SHA256) and AES-GCM with a 96-bit nonce.
  - The resulting security.config will be a JSON object:
    {
      "masterPasswordHash": "...",
      "authorizedUsers": [...],
      "encryptedData": "{ \"salt\":\"...\",\"iv\":\"...\",\"tag\":\"...\",\"data\":\"...\" }",
      "lastModified": "...",
      "encryptionMode": "passphrase"
    }
  - Clients must have the same passphrase available (installer writes this into settings.json Passphrase) to decrypt.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$SharedSecurityPath,

    [Parameter(Mandatory = $false)]
    [string]$Passphrase,

    [Parameter(Mandatory = $false)]
    [string]$MasterPassword,

    [Parameter(Mandatory = $false)]
    [string]$AuthorizedUsers = "",

    [Parameter(Mandatory = $false)]
    [string]$AIMInstallPath = "",

    [switch]$Backup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Prompt-SecureStringAsPlainText([string]$prompt) {
    $secure = Read-Host -AsSecureString $prompt
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

# Ask for passphrase if missing
if (-not $Passphrase) {
    Write-Host "Enter passphrase to use for encrypting the shared security config (will not echo):"
    $Passphrase = Prompt-SecureStringAsPlainText "Passphrase"
}

if ([string]::IsNullOrWhiteSpace($Passphrase)) {
    Write-Error "Passphrase is required. Aborting."
    exit 2
}

# Ask for master password if missing
if (-not $MasterPassword) {
    Write-Host "Enter the current master password (cleartext) for the security config (will not echo):"
    $MasterPassword = Prompt-SecureStringAsPlainText "Master password"
}

if ([string]::IsNullOrWhiteSpace($MasterPassword)) {
    Write-Error "Master password is required. Aborting."
    exit 3
}

# Normalize and parse authorized users
[int]$dummy = 0
$authUsersList = @()
if (-not [string]::IsNullOrWhiteSpace($AuthorizedUsers)) {
    # Accept comma-separated or semicolon-separated
    $sepUsers = $AuthorizedUsers -split '[,;]' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
    $authUsersList += $sepUsers
}

# Ensure shared path exists
if (-not (Test-Path -Path $SharedSecurityPath)) {
    Write-Host "Shared path does not exist. Creating: $SharedSecurityPath"
    New-Item -ItemType Directory -Path $SharedSecurityPath -Force | Out-Null
}

# Determine target config path
$targetConfigPath = Join-Path $SharedSecurityPath "security.config"

# Backup existing file if present
if (Test-Path $targetConfigPath) {
    $timestamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
    $backupPath = "$targetConfigPath.$timestamp.bak"
    Copy-Item -Path $targetConfigPath -Destination $backupPath -Force
    Write-Host "Backed up existing security.config -> $backupPath"
}

# Compute SHA256 hash of master password (Base64) - to match HashPassword behavior
function Get-Sha256Base64([string]$text) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $hash = $sha.ComputeHash($bytes)
    return [Convert]::ToBase64String($hash)
}

$masterPasswordHash = Get-Sha256Base64 $MasterPassword

# Build security data JSON
$securityData = @{
    masterPassword = $MasterPassword
    authorizedUsers = $authUsersList
}
$securityDataJson = ($securityData | ConvertTo-Json -Depth 10 -Compress)

# Prepare passphrase-based AES-GCM encryption
# Generate salt (16 bytes) and iv (12 bytes)
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
[byte[]]$salt = New-Object byte[] 16
[byte[]]$iv = New-Object byte[] 12
$null = $rng.GetBytes($salt)
$null = $rng.GetBytes($iv)

# Derive key using PBKDF2 (100000 iterations, SHA256)
$iterationCount = 100000
$kdf = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Passphrase, $salt, $iterationCount, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
[byte[]]$key = $kdf.GetBytes(32)

# Convert plaintext to bytes
[byte[]]$plaintextBytes = [System.Text.Encoding]::UTF8.GetBytes($securityDataJson)
[byte[]]$ciphertext = New-Object byte[] ($plaintextBytes.Length)
[byte[]]$tag = New-Object byte[] 16

# Use AES-GCM (requires .NET Core 3.0+ / PowerShell 7+)
try {
    $aes = [System.Security.Cryptography.AesGcm]::new($key)
    $aes.Encrypt($iv, $plaintextBytes, $ciphertext, $tag, $null)
    $aes.Dispose()
}
catch {
    Write-Error "AES-GCM encryption failed. Ensure you are running PowerShell 7+ on .NET Core that supports AesGcm. Error: $_"
    exit 4
}

# Build payload JSON with base64 fields
$payload = @{
    salt = [Convert]::ToBase64String($salt)
    iv   = [Convert]::ToBase64String($iv)
    tag  = [Convert]::ToBase64String($tag)
    data = [Convert]::ToBase64String($ciphertext)
}
$payloadJson = $payload | ConvertTo-Json -Depth 5

# Compose EncryptedSecurityConfig object
$configObj = @{
    masterPasswordHash = $masterPasswordHash
    authorizedUsers = $authUsersList
    encryptedData = $payloadJson
    lastModified = (Get-Date).ToUniversalTime().ToString("o")
    encryptionMode = "passphrase"
}

$configJson = $configObj | ConvertTo-Json -Depth 10

# Write the new config file
try {
    $configJson | Out-File -FilePath $targetConfigPath -Encoding UTF8 -Force
    Write-Host "Wrote new passphrase-encrypted security.config to: $targetConfigPath"
}
catch {
    Write-Error "Failed to write security.config: $_"
    exit 5
}

# Optionally write a local security-config.ini in the AIM install path so clients pick up shared path immediately
if (-not [string]::IsNullOrWhiteSpace($AIMInstallPath)) {
    try {
        if (-not (Test-Path -Path $AIMInstallPath)) {
            Write-Host "AIM install path does not exist. Creating: $AIMInstallPath"
            New-Item -ItemType Directory -Path $AIMInstallPath -Force | Out-Null
        }
        $localIniPath = Join-Path $AIMInstallPath "security-config.ini"
        $iniContent = @"
# AIM security-config.ini created by Deploy-AIM-Reencrypt
SharedSecurityPath=$SharedSecurityPath
"@
        $iniContent | Out-File -FilePath $localIniPath -Encoding UTF8 -Force
        Write-Host "Wrote local security-config.ini: $localIniPath"
    }
    catch {
        Write-Warning "Could not write local security-config.ini: $_"
    }
}

Write-Host "Re-encryption complete. Make sure clients have the same passphrase available (installer settings.json Passphrase)."
exit 0