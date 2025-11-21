<#
.SYNOPSIS
    Directory provisioning utility for AIM.
.DESCRIPTION
    This optional utility script validates and creates network directories needed for AIM operation.
    It accepts directory path parameters, validates that each path is accessible or creatable,
    creates the directories if they don't exist, and displays a summary of created/validated directories.
    This script is designed for pre-provisioning network directories before AIM installation.
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
    .\Deploy-AIM.ps1 -DefaultRootDirectory 'C:\AIM\Data' -ArchivePath 'C:\AIM\Archive' -ShippedDirectory 'C:\AIM\Shipped' -FileScansDirectory 'C:\AIM\FileScans' -InventoryArchiveDirectory 'C:\AIM\InventoryArchive'
.EXAMPLE
    .\Deploy-AIM.ps1 -DefaultRootDirectory '\\server\share\LAB_STOCK' -ArchivePath '\\server\share\Archive' -ShippedDirectory '\\server\share\Shipped' -FileScansDirectory 'C:\Tfile' -InventoryArchiveDirectory '\\server\share\Inventory_Archive'
#>

param (
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
    $DefaultRootDirectory,
    $ArchivePath,
    $ShippedDirectory,
    $FileScansDirectory,
    $InventoryArchiveDirectory
)

foreach ($dir in $directories) {
    Validate-And-Create-Directory -path $dir
}

# Summary of validated/created directories
Write-Host ""
Write-Host "==================================="
Write-Host "Directory Provisioning Summary"
Write-Host "==================================="
Write-Host "Default Root Directory: $DefaultRootDirectory" 
Write-Host "Archive Path: $ArchivePath" 
Write-Host "Shipped Directory: $ShippedDirectory" 
Write-Host "File Scans Directory: $FileScansDirectory" 
Write-Host "Inventory Archive Directory: $InventoryArchiveDirectory" 
Write-Host "==================================="
Write-Host ""
Write-Host "Directory provisioning completed successfully."
