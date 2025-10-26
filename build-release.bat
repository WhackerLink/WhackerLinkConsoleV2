@echo off
REM WhackerLink Console V2 Release Build Script (Batch)
REM Simple alternative to PowerShell script

echo ========================================
echo WhackerLink Console V2 - Release Builder
echo ========================================
echo.

cd WhackerLinkConsoleV2

echo [1/5] Cleaning previous builds...
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul
echo   Done

echo [2/5] Restoring NuGet packages...
dotnet restore
if errorlevel 1 goto error

echo [3/5] Building Release configuration...
dotnet build --configuration Release --no-incremental
if errorlevel 1 goto error

echo [4/5] Publishing release...
dotnet publish --configuration Release --output "bin\Publish" --self-contained false
if errorlevel 1 goto error

cd ..

echo [5/5] Creating release package...
rmdir /s /q release-package 2>nul
mkdir release-package
xcopy /E /I /Y WhackerLinkConsoleV2\bin\Publish release-package
copy RELEASE-NOTES.md release-package\ 2>nul
copy README.md release-package\ 2>nul
copy LICENSE release-package\ 2>nul

echo.
echo ========================================
echo Release Package Ready!
echo ========================================
echo Location: release-package
echo Executable: release-package\WhackerLinkConsoleV2.exe
echo.
echo Next steps:
echo   1. Test the executable
echo   2. Compress release-package folder to ZIP
echo   3. Distribute or upload to GitHub
echo.
pause
goto end

:error
echo.
echo ========================================
echo Build FAILED!
echo ========================================
echo Please check the error messages above
pause
exit /b 1

:end
