@echo off
REM Build WhackerLinkConsoleV2 as a self-contained standalone executable
REM Includes .NET runtime and all dependencies - no installation required!
echo ========================================
echo Building WhackerLinkConsoleV2 - Standalone
echo Self-Contained with .NET Runtime
echo Preserving UserSettings.json and codeplug files
echo ========================================
echo.

REM Set build configuration
set CONFIG=Release
set RUNTIME=win-x64
set PROJECT_DIR=%~dp0WhackerLinkConsoleV2
set OUTPUT_DIR=%~dp0WhackerLinkConsoleV2\bin\%CONFIG%\net8.0-windows\%RUNTIME%\publish
set BACKUP_DIR=%~dp0_backup_temp

REM Create backup directory
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

REM Backup existing UserSettings.json if it exists
if exist "%PROJECT_DIR%\UserSettings.json" (
    echo Backing up UserSettings.json...
    copy "%PROJECT_DIR%\UserSettings.json" "%BACKUP_DIR%\UserSettings.json" >nul
    echo   ✓ UserSettings.json backed up
)

REM Backup gabagool.yml if it exists in codeplugs
if exist "%PROJECT_DIR%\codeplugs\gabagool.yml" (
    echo Backing up gabagool.yml...
    copy "%PROJECT_DIR%\codeplugs\gabagool.yml" "%BACKUP_DIR%\gabagool.yml" >nul
    echo   ✓ gabagool.yml backed up
)

REM Backup auth_keys.yml if it exists
if exist "%PROJECT_DIR%\auth_keys.yml" (
    echo Backing up auth_keys.yml...
    copy "%PROJECT_DIR%\auth_keys.yml" "%BACKUP_DIR%\auth_keys.yml" >nul
    echo   ✓ auth_keys.yml backed up
)

echo.
echo Building self-contained standalone executable...
echo This may take a few minutes...
echo.

REM Publish as self-contained with single-file option
dotnet publish "%~dp0WhackerLinkConsoleV2.sln" ^
    --configuration %CONFIG% ^
    --runtime %RUNTIME% ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ✗ Build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ✓ Build successful!
echo.

REM Restore UserSettings.json to output directory
if exist "%BACKUP_DIR%\UserSettings.json" (
    echo Restoring UserSettings.json to output...
    copy "%BACKUP_DIR%\UserSettings.json" "%OUTPUT_DIR%\UserSettings.json" >nul
    echo   ✓ UserSettings.json restored
)

REM Restore gabagool.yml to output codeplugs directory
if exist "%BACKUP_DIR%\gabagool.yml" (
    if not exist "%OUTPUT_DIR%\codeplugs" mkdir "%OUTPUT_DIR%\codeplugs"
    echo Restoring gabagool.yml to output...
    copy "%BACKUP_DIR%\gabagool.yml" "%OUTPUT_DIR%\codeplugs\gabagool.yml" >nul
    echo   ✓ gabagool.yml restored
)

REM Restore auth_keys.yml to output directory
if exist "%BACKUP_DIR%\auth_keys.yml" (
    echo Restoring auth_keys.yml to output...
    copy "%BACKUP_DIR%\auth_keys.yml" "%OUTPUT_DIR%\auth_keys.yml" >nul
    echo   ✓ auth_keys.yml restored
)

REM Clean up backup directory
rmdir /S /Q "%BACKUP_DIR%"

echo.
echo ========================================
echo Standalone Build Complete!
echo ========================================
echo.
echo Output location:
echo %OUTPUT_DIR%
echo.
echo Main executable:
echo WhackerLinkConsoleV2.exe
echo.
echo This build includes:
echo   ✓ .NET 8.0 Runtime (no installation needed)
echo   ✓ All dependencies
echo   ✓ Your settings preserved
echo.
echo You can copy the entire folder to any Windows PC!
echo.

REM Get folder size
for /f "tokens=3" %%a in ('dir "%OUTPUT_DIR%" ^| findstr "File(s)"') do set SIZE=%%a
echo Folder size: ~%SIZE% bytes
echo.

if exist "%OUTPUT_DIR%\UserSettings.json" echo   ✓ UserSettings.json
if exist "%OUTPUT_DIR%\codeplugs\gabagool.yml" echo   ✓ codeplugs\gabagool.yml
if exist "%OUTPUT_DIR%\auth_keys.yml" echo   ✓ auth_keys.yml
echo.
echo Press any key to open the output folder...
pause >nul
explorer "%OUTPUT_DIR%"
