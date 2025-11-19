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
#>

param (
    [Parameter(Mandatory = $true)]
    [string]$AIMInstallPath,

    [Parameter(Mandatory = $true)]
    [string]$SharedSecurityPath,

    [Parameter(Mandatory = $true)]
    [string]$DefaultRootDirectory,

    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [Parameter(Mandatory = $true)]
    [string]$ShippedDirectory,

    [Parameter(Mandatory = $true)]
    [string]$FileScansDirectory,

    [Parameter(Mandatory = $true)]
    [string]$InventoryArchiveDirectory
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
    $SharedSecurityPath,
    $DefaultRootDirectory,
    $ArchivePath,
    $ShippedDirectory,
    $FileScansDirectory,
    $InventoryArchiveDirectory
)

foreach ($dir in $directories) {
    Validate-And-Create-Directory -path $dir
}

# Summary of the configuration
Write-Host "Configuration Summary:" 
Write-Host "AIM Install Path: $AIMInstallPath" 
Write-Host "Shared Security Path: $SharedSecurityPath" 
Write-Host "Default Root Directory: $DefaultRootDirectory" 
Write-Host "Archive Path: $ArchivePath" 
Write-Host "Shipped Directory: $ShippedDirectory" 
Write-Host "File Scans Directory: $FileScansDirectory" 
Write-Host "Inventory Archive Directory: $InventoryArchiveDirectory" 

Write-Host "Deployment script executed successfully."