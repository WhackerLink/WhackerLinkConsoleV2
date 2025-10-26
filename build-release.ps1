# WhackerLink Console V2 Release Build Script
# This script builds and packages WhackerLinkConsoleV2 for release

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\release-package"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "WhackerLink Console V2 - Release Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
Write-Host "[1/6] Cleaning previous builds..." -ForegroundColor Yellow
Set-Location WhackerLinkConsoleV2
Remove-Item -Recurse -Force bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force obj -ErrorAction SilentlyContinue
Write-Host "  ✓ Cleaned" -ForegroundColor Green

# Step 2: Restore NuGet packages
Write-Host "[2/6] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Packages restored" -ForegroundColor Green

# Step 3: Build Release configuration
Write-Host "[3/6] Building Release configuration..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Build successful" -ForegroundColor Green

# Step 4: Publish self-contained release
Write-Host "[4/6] Publishing release..." -ForegroundColor Yellow
dotnet publish --configuration $Configuration --output ".\bin\Publish" --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Published" -ForegroundColor Green

# Step 5: Create release package directory
Write-Host "[5/6] Creating release package..." -ForegroundColor Yellow
Set-Location ..
Remove-Item -Recurse -Force $OutputDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# Copy published files
Copy-Item -Recurse "WhackerLinkConsoleV2\bin\Publish\*" "$OutputDir\"

# Copy additional resources
Copy-Item "README.md" "$OutputDir\" -ErrorAction SilentlyContinue
Copy-Item "LICENSE" "$OutputDir\" -ErrorAction SilentlyContinue

Write-Host "  ✓ Package created at: $OutputDir" -ForegroundColor Green

# Step 6: Get version info
Write-Host "[6/6] Getting version info..." -ForegroundColor Yellow
$gitHash = git rev-parse --short HEAD
$gitBranch = git rev-parse --abbrev-ref HEAD
$buildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Release Package Ready!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Location:    $OutputDir" -ForegroundColor White
Write-Host "Git Branch:  $gitBranch" -ForegroundColor White
Write-Host "Git Commit:  $gitHash" -ForegroundColor White
Write-Host "Build Date:  $buildDate" -ForegroundColor White
Write-Host ""
Write-Host "Executable:  $OutputDir\WhackerLinkConsoleV2.exe" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Test the executable in $OutputDir" -ForegroundColor White
Write-Host "  2. Compress the folder to a ZIP file for distribution" -ForegroundColor White
Write-Host "  3. Create a GitHub release and upload the ZIP" -ForegroundColor White
Write-Host ""
