@echo off
REM Build WhackerLinkConsoleV2 while preserving settings and codeplug files
echo ========================================
echo Building WhackerLinkConsoleV2
echo Preserving UserSettings.json and codeplug files
echo ========================================
echo.

REM Set build configuration
set CONFIG=Release
set PROJECT_DIR=%~dp0WhackerLinkConsoleV2
set OUTPUT_DIR=%~dp0WhackerLinkConsoleV2\bin\%CONFIG%\net8.0-windows
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
echo Building project...
echo.

REM Build the project
dotnet build "%~dp0WhackerLinkConsoleV2.sln" --configuration %CONFIG%

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
    echo   ✓ UserSettings.json restored to: %OUTPUT_DIR%
)

REM Restore gabagool.yml to output codeplugs directory
if exist "%BACKUP_DIR%\gabagool.yml" (
    if not exist "%OUTPUT_DIR%\codeplugs" mkdir "%OUTPUT_DIR%\codeplugs"
    echo Restoring gabagool.yml to output...
    copy "%BACKUP_DIR%\gabagool.yml" "%OUTPUT_DIR%\codeplugs\gabagool.yml" >nul
    echo   ✓ gabagool.yml restored to: %OUTPUT_DIR%\codeplugs
)

REM Restore auth_keys.yml to output directory
if exist "%BACKUP_DIR%\auth_keys.yml" (
    echo Restoring auth_keys.yml to output...
    copy "%BACKUP_DIR%\auth_keys.yml" "%OUTPUT_DIR%\auth_keys.yml" >nul
    echo   ✓ auth_keys.yml restored to: %OUTPUT_DIR%
)

REM Clean up backup directory
rmdir /S /Q "%BACKUP_DIR%"

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Executable location:
echo %OUTPUT_DIR%\WhackerLinkConsoleV2.exe
echo.
echo Your settings have been preserved:
if exist "%OUTPUT_DIR%\UserSettings.json" echo   ✓ UserSettings.json
if exist "%OUTPUT_DIR%\codeplugs\gabagool.yml" echo   ✓ codeplugs\gabagool.yml
if exist "%OUTPUT_DIR%\auth_keys.yml" echo   ✓ auth_keys.yml
echo.
echo Press any key to open the output folder...
pause >nul
explorer "%OUTPUT_DIR%"
