#!/usr/bin/env pwsh
# Build WhackerLinkConsoleV2 as a self-contained standalone executable
# Includes .NET runtime and all dependencies - no installation required!

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building WhackerLinkConsoleV2 - Standalone" -ForegroundColor Cyan
Write-Host "Self-Contained with .NET Runtime" -ForegroundColor Cyan
Write-Host "Preserving UserSettings.json and codeplug files" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set build configuration
$config = "Release"
$runtime = "win-x64"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "WhackerLinkConsoleV2"
$outputDir = Join-Path $projectDir "bin\$config\net8.0-windows\$runtime\publish"
$backupDir = Join-Path $scriptDir "_backup_temp"

# Create backup directory
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}

# Backup existing UserSettings.json if it exists
$userSettingsPath = Join-Path $projectDir "UserSettings.json"
if (Test-Path $userSettingsPath) {
    Write-Host "Backing up UserSettings.json..." -ForegroundColor Yellow
    Copy-Item $userSettingsPath (Join-Path $backupDir "UserSettings.json")
    Write-Host "  ✓ UserSettings.json backed up" -ForegroundColor Green
}

# Backup gabagool.yml if it exists in codeplugs
$gabagoolPath = Join-Path $projectDir "codeplugs\gabagool.yml"
if (Test-Path $gabagoolPath) {
    Write-Host "Backing up gabagool.yml..." -ForegroundColor Yellow
    Copy-Item $gabagoolPath (Join-Path $backupDir "gabagool.yml")
    Write-Host "  ✓ gabagool.yml backed up" -ForegroundColor Green
}

# Backup auth_keys.yml if it exists
$authKeysPath = Join-Path $projectDir "auth_keys.yml"
if (Test-Path $authKeysPath) {
    Write-Host "Backing up auth_keys.yml..." -ForegroundColor Yellow
    Copy-Item $authKeysPath (Join-Path $backupDir "auth_keys.yml")
    Write-Host "  ✓ auth_keys.yml backed up" -ForegroundColor Green
}

Write-Host ""
Write-Host "Building self-contained standalone executable..." -ForegroundColor Cyan
Write-Host "This may take a few minutes..." -ForegroundColor Yellow
Write-Host ""

# Publish as self-contained
$solutionPath = Join-Path $scriptDir "WhackerLinkConsoleV2.sln"
dotnet publish $solutionPath `
    --configuration $config `
    --runtime $runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "✗ Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "✓ Build successful!" -ForegroundColor Green
Write-Host ""

# Copy Audio files to root output directory
Write-Host "Copying Audio files to root..." -ForegroundColor Yellow
$audioSourceDir = Join-Path $projectDir "Audio"
if (Test-Path $audioSourceDir) {
    Copy-Item "$audioSourceDir\*.wav" $outputDir -Force
    Write-Host "  ✓ Audio files copied to root" -ForegroundColor Green
} else {
    Write-Host "  ! Audio folder not found" -ForegroundColor Yellow
}

# Copy gabagool.yml to root output directory
Write-Host "Copying gabagool.yml to root..." -ForegroundColor Yellow
$gabagoolPath = Join-Path $projectDir "codeplugs\gabagool.yml"
$codeplugPath = Join-Path $projectDir "codeplugs\codeplug.yml"
if (Test-Path $gabagoolPath) {
    Copy-Item $gabagoolPath (Join-Path $outputDir "gabagool.yml") -Force
    Write-Host "  ✓ gabagool.yml copied to root" -ForegroundColor Green
} elseif (Test-Path $codeplugPath) {
    Copy-Item $codeplugPath (Join-Path $outputDir "gabagool.yml") -Force
    Write-Host "  ✓ codeplug.yml copied as gabagool.yml to root" -ForegroundColor Green
} else {
    Write-Host "  ! No codeplug file found" -ForegroundColor Yellow
}

# Restore UserSettings.json to output directory
$backupUserSettings = Join-Path $backupDir "UserSettings.json"
if (Test-Path $backupUserSettings) {
    Write-Host "Restoring UserSettings.json to output..." -ForegroundColor Yellow
    Copy-Item $backupUserSettings (Join-Path $outputDir "UserSettings.json")
    Write-Host "  ✓ UserSettings.json restored" -ForegroundColor Green
}

# Restore gabagool.yml if it was backed up (overrides the copied version)
$backupGabagool = Join-Path $backupDir "gabagool.yml"
if (Test-Path $backupGabagool) {
    Write-Host "Restoring gabagool.yml to root..." -ForegroundColor Yellow
    Copy-Item $backupGabagool (Join-Path $outputDir "gabagool.yml")
    Write-Host "  ✓ gabagool.yml restored to root" -ForegroundColor Green
}

# Restore auth_keys.yml to output directory
$backupAuthKeys = Join-Path $backupDir "auth_keys.yml"
if (Test-Path $backupAuthKeys) {
    Write-Host "Restoring auth_keys.yml to output..." -ForegroundColor Yellow
    Copy-Item $backupAuthKeys (Join-Path $outputDir "auth_keys.yml")
    Write-Host "  ✓ auth_keys.yml restored" -ForegroundColor Green
}

# Clean up backup directory
Remove-Item $backupDir -Recurse -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Standalone Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output location:" -ForegroundColor Cyan
Write-Host "$outputDir" -ForegroundColor White
Write-Host ""
Write-Host "Main executable:" -ForegroundColor Cyan
Write-Host "WhackerLinkConsoleV2.exe" -ForegroundColor White
Write-Host ""
Write-Host "This build includes:" -ForegroundColor Cyan
Write-Host "  ✓ .NET 8.0 Runtime (no installation needed)" -ForegroundColor Green
Write-Host "  ✓ All dependencies" -ForegroundColor Green
Write-Host "  ✓ Your settings preserved" -ForegroundColor Green
Write-Host ""
Write-Host "You can copy the entire folder to any Windows PC!" -ForegroundColor Yellow
Write-Host ""

# Get folder size
$folderSize = (Get-ChildItem $outputDir -Recurse | Measure-Object -Property Length -Sum).Sum
$folderSizeMB = [math]::Round($folderSize / 1MB, 2)
Write-Host "Folder size: ~$folderSizeMB MB" -ForegroundColor Cyan
Write-Host ""

if (Test-Path (Join-Path $outputDir "UserSettings.json")) {
    Write-Host "  ✓ UserSettings.json" -ForegroundColor Green
}
if (Test-Path (Join-Path $outputDir "gabagool.yml")) {
    Write-Host "  ✓ gabagool.yml" -ForegroundColor Green
}
if (Test-Path (Join-Path $outputDir "emergency.wav")) {
    Write-Host "  ✓ Audio files (*.wav)" -ForegroundColor Green
}
if (Test-Path (Join-Path $outputDir "auth_keys.yml")) {
    Write-Host "  ✓ auth_keys.yml" -ForegroundColor Green
}
Write-Host ""
Write-Host "Opening output folder..." -ForegroundColor Cyan
Start-Process explorer.exe $outputDir
