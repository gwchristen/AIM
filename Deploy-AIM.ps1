<#
.SYNOPSIS
    Deployment script for AIM configuration.
.DESCRIPTION
    This script configures directory paths and shared security configurations
    for the AIM application. It validates paths, creates directories as needed,
    and displays a summary of the configuration settings.
.PARAMETER AIMInstallPath
    The installation path for the AIM application.
.PARAMETER SharedSecurityPath
    The path that contains shared security configurations.
.PARAMETER Passphrase
    Optional passphrase for encrypting shared security configuration.
    If provided, creates a passphrase-encrypted config that can be shared across users.
    If not provided, uses DPAPI encryption (user/machine specific).
.PARAMETER DefaultRootDirectory
    The default root directory for AIM data.
.PARAMETER ArchivePath
    The path where archived data will be stored.
.PARAMETER ShippedDirectory
    The directory where shipped files are located.
.PARAMETER FileScansDirectory
    The directory for file scans.
.PARAMETER InventoryArchiveDirectory
    The directory for archived inventory data.
.EXAMPLE
    .\Deploy-AIM.ps1 -AIMInstallPath 'C:\AIM' -SharedSecurityPath 'C:\SharedSecurity' -DefaultRootDirectory 'C:\AIM\Data' -ArchivePath 'C:\AIM\Archive' -ShippedDirectory 'C:\AIM\Shipped' -FileScansDirectory 'C:\AIM\FileScans' -InventoryArchiveDirectory 'C:\AIM\InventoryArchive'
.EXAMPLE
    .\Deploy-AIM.ps1 -AIMInstallPath 'C:\AIM' -SharedSecurityPath '\\server\share\AIM_Security' -Passphrase 'MySecurePassphrase123!' -DefaultRootDirectory 'C:\AIM\Data' -ArchivePath 'C:\AIM\Archive' -ShippedDirectory 'C:\AIM\Shipped' -FileScansDirectory 'C:\AIM\FileScans' -InventoryArchiveDirectory 'C:\AIM\InventoryArchive'
#>

param (
    [Parameter(Mandatory = $true)]
    [string]$AIMInstallPath = "C:\Program Files\AIM",

    [Parameter(Mandatory = $false)]
    [string]$SharedSecurityPath = "\\oh1cam01\cml\Internal\LAB STOCK\Important Inventory Related Documents\AIM\AIM_Security",

    [Parameter(Mandatory = $false)]
    [string]$Passphrase = "",

    [Parameter(Mandatory = $true)]
    [string]$DefaultRootDirectory = "\\oh1cam01\cml\Internal\LAB STOCK\LAB STOCK",

    [Parameter(Mandatory = $true)]
    [string]$ArchivePath = "\\oh1cam01\cml\Internal\LAB STOCK\Archive",

    [Parameter(Mandatory = $true)]
    [string]$ShippedDirectory = "\\oh1cam01\cml\Internal\LAB STOCK\Orders shipped",

    [Parameter(Mandatory = $true)]
    [string]$FileScansDirectory = "C:\Tfile",

    [Parameter(Mandatory = $true)]
    [string]$InventoryArchiveDirectory = "\\oh1cam01\cml\Internal\LAB STOCK\Physical Inventory Archive"
)

# Function to validate directory paths and create them if they do not exist
function Validate-And-Create-Directory {
    param (
        [string]$path
    )
    if (-not (Test-Path -Path $path)) {
        New-Item -Path $path -ItemType Directory -Force
        Write-Host "Created directory: $path"
    } else {
        Write-Host "Directory already exists: $path"
    }
}

# Validate and create necessary directories
$directories = @(
    $AIMInstallPath,
    $DefaultRootDirectory,
    $ArchivePath,
    $ShippedDirectory,
    $FileScansDirectory,
    $InventoryArchiveDirectory
)

# Add SharedSecurityPath only if provided
if (-not [string]::IsNullOrWhiteSpace($SharedSecurityPath)) {
    $directories += $SharedSecurityPath
}

foreach ($dir in $directories) {
    Validate-And-Create-Directory -path $dir
}

# Summary of the configuration
Write-Host "Configuration Summary:" 
Write-Host "AIM Install Path: $AIMInstallPath" 
if (-not [string]::IsNullOrWhiteSpace($SharedSecurityPath)) {
    Write-Host "Shared Security Path: $SharedSecurityPath" 
    if ($Passphrase) {
        Write-Host "Passphrase: ********** (provided - will use passphrase-based encryption)"
    } else {
        Write-Host "Passphrase: (not provided - will use DPAPI encryption)"
    }
} else {
    Write-Host "Shared Security Path: (not configured - using local security)"
}
Write-Host "Default Root Directory: $DefaultRootDirectory" 
Write-Host "Archive Path: $ArchivePath" 
Write-Host "Shipped Directory: $ShippedDirectory" 
Write-Host "File Scans Directory: $FileScansDirectory" 
Write-Host "Inventory Archive Directory: $InventoryArchiveDirectory" 

Write-Host "Deployment script executed successfully."
