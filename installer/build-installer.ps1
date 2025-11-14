$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$publishRoot = Join-Path $PSScriptRoot 'staging'
$distRoot = Join-Path $PSScriptRoot 'dist'
$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

Write-Host "AuroraSwitch packaging pipeline" -ForegroundColor Cyan
Write-Host "Build started at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI not found on PATH. Install .NET SDK 8.0+ and retry."
}

Write-Host "dotnet version:"
dotnet --info | Select-String 'Version'

if (Test-Path $publishRoot) {
    Write-Host "Cleaning previous staging output..."
    Remove-Item $publishRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path (Join-Path $publishRoot 'Dashboard') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $publishRoot 'HostService') | Out-Null

Write-Host "Publishing WPF dashboard..."
dotnet publish `
    (Join-Path $root 'src\KvmSwitch.Dashboard\KvmSwitch.Dashboard.csproj') `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output (Join-Path $publishRoot 'Dashboard')

Write-Host "Publishing host service..."
dotnet publish `
    (Join-Path $root 'src\KvmSwitch.HostService\KvmSwitch.HostService.csproj') `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output (Join-Path $publishRoot 'HostService')

# Validate publish outputs
Write-Host ""
Write-Host "Validating publish outputs..." -ForegroundColor Gray

$dashboardExe = Join-Path $publishRoot 'Dashboard\KvmSwitch.Dashboard.exe'
$hostServiceExe = Join-Path $publishRoot 'HostService\KvmSwitch.HostService.exe'

if (-not (Test-Path $dashboardExe)) {
    throw "Dashboard publish output missing expected executable: $dashboardExe"
}
if (-not (Test-Path $hostServiceExe)) {
    throw "Host service publish output missing expected executable: $hostServiceExe"
}

# Report file counts and sizes
$dashboardFiles = (Get-ChildItem (Join-Path $publishRoot 'Dashboard') -Recurse -File).Count
$hostServiceFiles = (Get-ChildItem (Join-Path $publishRoot 'HostService') -Recurse -File).Count
$dashboardSize = [math]::Round((Get-ChildItem (Join-Path $publishRoot 'Dashboard') -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
$hostServiceSize = [math]::Round((Get-ChildItem (Join-Path $publishRoot 'HostService') -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 2)

Write-Host "  Dashboard: $dashboardFiles files, $dashboardSize MB" -ForegroundColor Cyan
Write-Host "  HostService: $hostServiceFiles files, $hostServiceSize MB" -ForegroundColor Cyan

$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup (ISCC.exe) not found. Install from https://jrsoftware.org/isdl.php."
}

if (Test-Path $distRoot) {
    Write-Host "Cleaning previous dist output..."
    Remove-Item $distRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $distRoot | Out-Null

Write-Host ""
Write-Host "Compiling installer with Inno Setup..." -ForegroundColor Gray
Write-Host "  Using: $iscc" -ForegroundColor Cyan
$issPath = Join-Path $PSScriptRoot 'auroraswitch-setup.iss'
if (-not (Test-Path $issPath)) {
    throw "Inno Setup script not found: $issPath"
}

$issProcess = Start-Process -FilePath $iscc -ArgumentList "`"$issPath`"", "/O`"$distRoot`"" -Wait -PassThru -NoNewWindow
if ($issProcess.ExitCode -ne 0) {
    throw "Inno Setup compilation failed with exit code $($issProcess.ExitCode). Check the script for errors."
}

$installerPath = Get-ChildItem $distRoot -Filter '*.exe' | Select-Object -First 1
if ($installerPath) {
    Write-Host ""
    Write-Host "Installer created successfully!" -ForegroundColor Green
    Write-Host "  Path: $($installerPath.FullName)" -ForegroundColor Cyan
    $fileSize = [math]::Round($installerPath.Length / 1MB, 2)
    Write-Host "  Size: $fileSize MB" -ForegroundColor Cyan
    
    # Extract version from Dashboard executable
    try {
        if (Test-Path $dashboardExe) {
            $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dashboardExe)
            Write-Host "  Version: $($versionInfo.FileVersion)" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "  Warning: Could not extract version information" -ForegroundColor Yellow
    }
    
    # Generate checksum
    Write-Host ""
    Write-Host "Generating SHA256 checksum..." -ForegroundColor Gray
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $fileStream = [System.IO.File]::OpenRead($installerPath.FullName)
    $hashBytes = $sha256.ComputeHash($fileStream)
    $fileStream.Close()
    $sha256.Dispose()
    $hashString = [System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLower()
    $checksumPath = Join-Path $distRoot "$($installerPath.BaseName).sha256"
    Set-Content -Path $checksumPath -Value "$hashString  $($installerPath.Name)"
    Write-Host "  Checksum: $checksumPath" -ForegroundColor Cyan
    Write-Host "  SHA256: $hashString" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "Build completed at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Green
} else {
    throw "Installer compilation completed but no executable found in dist directory."
}

Write-Host ""
Write-Host "Packaging complete." -ForegroundColor Green
