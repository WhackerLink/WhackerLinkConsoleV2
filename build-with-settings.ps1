#!/usr/bin/env pwsh
# Build WhackerLinkConsoleV2 while preserving settings and codeplug files

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building WhackerLinkConsoleV2" -ForegroundColor Cyan
Write-Host "Preserving UserSettings.json and codeplug files" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set build configuration
$config = "Release"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "WhackerLinkConsoleV2"
$outputDir = Join-Path $projectDir "bin\$config\net8.0-windows"
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
Write-Host "Building project..." -ForegroundColor Cyan
Write-Host ""

# Build the project
$solutionPath = Join-Path $scriptDir "WhackerLinkConsoleV2.sln"
dotnet build $solutionPath --configuration $config

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "✗ Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "✓ Build successful!" -ForegroundColor Green
Write-Host ""

# Restore UserSettings.json to output directory
$backupUserSettings = Join-Path $backupDir "UserSettings.json"
if (Test-Path $backupUserSettings) {
    Write-Host "Restoring UserSettings.json to output..." -ForegroundColor Yellow
    Copy-Item $backupUserSettings (Join-Path $outputDir "UserSettings.json")
    Write-Host "  ✓ UserSettings.json restored to: $outputDir" -ForegroundColor Green
}

# Restore gabagool.yml to output codeplugs directory
$backupGabagool = Join-Path $backupDir "gabagool.yml"
if (Test-Path $backupGabagool) {
    $outputCodeplugsDir = Join-Path $outputDir "codeplugs"
    if (-not (Test-Path $outputCodeplugsDir)) {
        New-Item -ItemType Directory -Path $outputCodeplugsDir | Out-Null
    }
    Write-Host "Restoring gabagool.yml to output..." -ForegroundColor Yellow
    Copy-Item $backupGabagool (Join-Path $outputCodeplugsDir "gabagool.yml")
    Write-Host "  ✓ gabagool.yml restored to: $outputCodeplugsDir" -ForegroundColor Green
}

# Restore auth_keys.yml to output directory
$backupAuthKeys = Join-Path $backupDir "auth_keys.yml"
if (Test-Path $backupAuthKeys) {
    Write-Host "Restoring auth_keys.yml to output..." -ForegroundColor Yellow
    Copy-Item $backupAuthKeys (Join-Path $outputDir "auth_keys.yml")
    Write-Host "  ✓ auth_keys.yml restored to: $outputDir" -ForegroundColor Green
}

# Clean up backup directory
Remove-Item $backupDir -Recurse -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Executable location:" -ForegroundColor Cyan
Write-Host "$outputDir\WhackerLinkConsoleV2.exe" -ForegroundColor White
Write-Host ""
Write-Host "Your settings have been preserved:" -ForegroundColor Cyan
if (Test-Path (Join-Path $outputDir "UserSettings.json")) {
    Write-Host "  ✓ UserSettings.json" -ForegroundColor Green
}
if (Test-Path (Join-Path $outputDir "codeplugs\gabagool.yml")) {
    Write-Host "  ✓ codeplugs\gabagool.yml" -ForegroundColor Green
}
if (Test-Path (Join-Path $outputDir "auth_keys.yml")) {
    Write-Host "  ✓ auth_keys.yml" -ForegroundColor Green
}
Write-Host ""
Write-Host "Opening output folder..." -ForegroundColor Cyan
Start-Process explorer.exe $outputDir
