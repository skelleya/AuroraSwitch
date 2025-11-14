# Launch AuroraSwitch Dashboard
# This script helps launch the Dashboard application from development or installed location

$ErrorActionPreference = 'Stop'

Write-Host "AuroraSwitch Dashboard Launcher" -ForegroundColor Cyan
Write-Host ""

# Try to find the Dashboard executable
$dashboardPaths = @(
    # Development build
    (Join-Path $PSScriptRoot "src\KvmSwitch.Dashboard\bin\Debug\net8.0-windows\KvmSwitch.Dashboard.exe"),
    (Join-Path $PSScriptRoot "src\KvmSwitch.Dashboard\bin\Release\net8.0-windows\KvmSwitch.Dashboard.exe"),
    # Installed location
    "${env:ProgramFiles}\AuroraSwitch\Dashboard\KvmSwitch.Dashboard.exe",
    "${env:ProgramFiles(x86)}\AuroraSwitch\Dashboard\KvmSwitch.Dashboard.exe"
)

$dashboardExe = $null
foreach ($path in $dashboardPaths) {
    if (Test-Path $path) {
        $dashboardExe = $path
        Write-Host "Found Dashboard at: $path" -ForegroundColor Green
        break
    }
}

if (-not $dashboardExe) {
    Write-Host "ERROR: Dashboard executable not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Searched locations:" -ForegroundColor Yellow
    foreach ($path in $dashboardPaths) {
        Write-Host "  - $path" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Please:" -ForegroundColor Yellow
    Write-Host "  1. Build the solution: dotnet build" -ForegroundColor Yellow
    Write-Host "  2. Or install the application using the installer" -ForegroundColor Yellow
    exit 1
}

# Check database directory
$dbDir = Join-Path $env:LOCALAPPDATA "KvmSwitch"
if (-not (Test-Path $dbDir)) {
    Write-Host "Creating database directory: $dbDir" -ForegroundColor Gray
    New-Item -ItemType Directory -Force -Path $dbDir | Out-Null
}

Write-Host "Database location: $dbDir\endpoints.db" -ForegroundColor Cyan
Write-Host ""

# Launch the Dashboard
Write-Host "Launching Dashboard..." -ForegroundColor Green
try {
    Start-Process -FilePath $dashboardExe -WorkingDirectory (Split-Path $dashboardExe)
    Write-Host "Dashboard launched successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "If the window doesn't appear:" -ForegroundColor Yellow
    Write-Host "  - Check Task Manager for KvmSwitch.Dashboard.exe" -ForegroundColor Yellow
    Write-Host "  - Try Alt+Tab to find the window" -ForegroundColor Yellow
    Write-Host "  - Check Event Viewer for errors" -ForegroundColor Yellow
} catch {
    Write-Host "ERROR: Failed to launch Dashboard" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try running directly:" -ForegroundColor Yellow
    Write-Host "  $dashboardExe" -ForegroundColor Cyan
    exit 1
}


