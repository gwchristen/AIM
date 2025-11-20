<#
.SYNOPSIS
    Build script for creating the AIM self-extracting installer.

.DESCRIPTION
    This script automates the process of building the AIM installer:
    1. Cleans previous builds
    2. Publishes the AIM application as a self-contained executable
    3. Creates a ZIP archive of the published application
    4. Copies the Deploy-AIM.ps1 script to the installer resources
    5. Builds the AIM.Installer project
    6. Outputs a single AIM-Installer.exe file

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release

.PARAMETER Runtime
    Target runtime identifier. Default: win-x64

.PARAMETER SkipClean
    Skip cleaning previous builds.

.EXAMPLE
    .\Build-Installer.ps1
    Builds the installer with default settings (Release, win-x64)

.EXAMPLE
    .\Build-Installer.ps1 -Configuration Debug -Runtime win-x86
    Builds a debug installer for x86
#>

param (
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",

    [Parameter(Mandatory = $false)]
    [switch]$SkipClean
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Script paths
$scriptRoot = $PSScriptRoot
$aimProjectPath = Join-Path $scriptRoot "AIM.csproj"
$installerProjectPath = Join-Path $scriptRoot "AIM.Installer\AIM.Installer.csproj"
$installerResourcesPath = Join-Path $scriptRoot "AIM.Installer\Resources"
$deployScriptSource = Join-Path $scriptRoot "Deploy-AIM.ps1"

# Output paths
$publishPath = Join-Path $scriptRoot "bin\Publish\$Runtime"
$zipPath = Join-Path $installerResourcesPath "AIM-Published.zip"
$deployScriptDest = Join-Path $installerResourcesPath "Deploy-AIM.ps1"
$installerOutputPath = Join-Path $scriptRoot "AIM.Installer\bin\$Configuration\net8.0-windows"
$finalInstallerPath = Join-Path $scriptRoot "bin\AIM-Installer-$Runtime.exe"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AIM Installer Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host ""

# Validate prerequisites
Write-Host "[1/7] Validating prerequisites..." -ForegroundColor Green
if (-not (Test-Path $aimProjectPath)) {
    throw "AIM project not found at: $aimProjectPath"
}
if (-not (Test-Path $installerProjectPath)) {
    throw "Installer project not found at: $installerProjectPath"
}
if (-not (Test-Path $deployScriptSource)) {
    Write-Host "Warning: Deploy-AIM.ps1 not found at: $deployScriptSource" -ForegroundColor Yellow
    Write-Host "The installer will be built without the deployment script." -ForegroundColor Yellow
}

# Check for dotnet CLI
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET SDK version: $dotnetVersion" -ForegroundColor Gray
}
catch {
    throw "dotnet CLI not found. Please install the .NET SDK."
}

# Clean previous builds
if (-not $SkipClean) {
    Write-Host ""
    Write-Host "[2/7] Cleaning previous builds..." -ForegroundColor Green
    
    if (Test-Path $publishPath) {
        Remove-Item -Path $publishPath -Recurse -Force
        Write-Host "Removed: $publishPath" -ForegroundColor Gray
    }
    
    if (Test-Path $installerResourcesPath) {
        Remove-Item -Path $installerResourcesPath -Recurse -Force
        Write-Host "Removed: $installerResourcesPath" -ForegroundColor Gray
    }
    
    # Clean AIM project
    Write-Host "Cleaning AIM project..." -ForegroundColor Gray
    dotnet clean $aimProjectPath -c $Configuration --nologo -v quiet
    
    # Clean Installer project
    Write-Host "Cleaning Installer project..." -ForegroundColor Gray
    dotnet clean $installerProjectPath -c $Configuration --nologo -v quiet
}
else {
    Write-Host ""
    Write-Host "[2/7] Skipping clean step..." -ForegroundColor Yellow
}

# Create resources directory
Write-Host ""
Write-Host "[3/7] Creating resources directory..." -ForegroundColor Green
New-Item -ItemType Directory -Path $installerResourcesPath -Force | Out-Null
Write-Host "Created: $installerResourcesPath" -ForegroundColor Gray

# Publish AIM application
Write-Host ""
Write-Host "[4/7] Publishing AIM application..." -ForegroundColor Green
Write-Host "This may take several minutes..." -ForegroundColor Gray

$publishArgs = @(
    "publish",
    $aimProjectPath, 
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-o", $publishPath,
    "-p:PublishSingleFile=false",
    "-p:PublishTrimmed=false",
    "-p:PublishReadyToRun=true",
    "--nologo",
    "-v", "minimal"
)

try {
    $publishOutput = & dotnet $publishArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish output:" -ForegroundColor Red
        Write-Host $publishOutput -ForegroundColor Red
        throw "Failed to publish AIM application. Exit code: $LASTEXITCODE"
    }
    Write-Host "AIM application published successfully." -ForegroundColor Gray
    Write-Host "Published to: $publishPath" -ForegroundColor Gray
}
catch {
    throw "Error publishing AIM application: $_"
}

# Create ZIP archive
Write-Host ""
Write-Host "[5/7] Creating ZIP archive..." -ForegroundColor Green

if (-not (Test-Path $publishPath)) {
    throw "Published application not found at: $publishPath"
}

try {
    # Remove old ZIP if exists
    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }

    # Create ZIP archive
    Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force
    
    $zipSize = (Get-Item $zipPath).Length / 1MB
    Write-Host "ZIP created: $zipPath" -ForegroundColor Gray
    Write-Host "ZIP size: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Gray
}
catch {
    throw "Error creating ZIP archive: $_"
}

# Copy Deploy-AIM.ps1 script
Write-Host ""
Write-Host "[6/7] Copying deployment script..." -ForegroundColor Green

if (Test-Path $deployScriptSource) {
    try {
        Copy-Item -Path $deployScriptSource -Destination $deployScriptDest -Force
        Write-Host "Deploy-AIM.ps1 copied to resources." -ForegroundColor Gray
    }
    catch {
        Write-Host "Warning: Could not copy Deploy-AIM.ps1: $_" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Skipping Deploy-AIM.ps1 (not found)." -ForegroundColor Yellow
}

# Build installer project
Write-Host ""
Write-Host "[7/7] Building installer..." -ForegroundColor Green
Write-Host "This may take a few minutes..." -ForegroundColor Gray

$buildArgs = @(
    "publish",
    $installerProjectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:PublishTrimmed=false",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:IncludeAllContentForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=embedded",
    "-o", $installerOutputPath,
    "--nologo",
    "-v", "minimal"
)

try {
    $buildOutput = & dotnet $buildArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build output:" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        throw "Failed to build installer. Exit code: $LASTEXITCODE"
    }
    Write-Host "Installer built successfully." -ForegroundColor Gray
}
catch {
    throw "Error building installer: $_"
}

# Find the output installer EXE
$installerExe = Get-ChildItem -Path $installerOutputPath -Filter "AIM-Installer.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $installerExe) {
    throw "Installer executable not found in: $installerOutputPath"
}

# Copy to final location
Write-Host ""
Write-Host "Finalizing installer..." -ForegroundColor Green

$binDir = Split-Path $finalInstallerPath -Parent
if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir -Force | Out-Null
}

Copy-Item -Path $installerExe.FullName -Destination $finalInstallerPath -Force

$installerSize = (Get-Item $finalInstallerPath).Length / 1MB
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installer location: $finalInstallerPath" -ForegroundColor Yellow
Write-Host "Installer size: $([math]::Round($installerSize, 2)) MB" -ForegroundColor Yellow
Write-Host ""
Write-Host "You can now distribute this single EXE file." -ForegroundColor Gray
Write-Host ""
