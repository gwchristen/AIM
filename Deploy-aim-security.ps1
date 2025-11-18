param(
    [string]$sharedNetworkPath = "\\oh1cam01\cmll Internal\Lab Stock\Lab Stock\AIM_Security\security.config",
    [string]$appFolder = "$env:LocalAppData\AIM\Security"
)

Write-Host "AIM Security Configuration Deployment Script" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green

# Check if shared config exists
if (-not (Test-Path $sharedNetworkPath)) {
    Write-Host "ERROR: Shared network config not found at: $sharedNetworkPath" -ForegroundColor Red
    Write-Host "Please ensure the path is correct and the network is accessible." -ForegroundColor Yellow
    exit 1
}

Write-Host "Found shared config at: $sharedNetworkPath" -ForegroundColor Green

# Create local directory if it doesn't exist
if (-not (Test-Path $appFolder)) {
    New-Item -ItemType Directory -Path $appFolder -Force | Out-Null
    Write-Host "Created directory: $appFolder" -ForegroundColor Green
}

# Copy the security config
try {
    Copy-Item -Path $sharedNetworkPath -Destination "$appFolder\security.config" -Force
    Write-Host "Successfully cached shared security config locally" -ForegroundColor Green
    Write-Host "Local cache location: $appFolder\security.config" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Could not copy security config: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "Users can now launch AIM without password prompts" -ForegroundColor Cyan
