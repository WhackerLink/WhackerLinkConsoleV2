# WhackerLink Console V2 - Packaging Guide

This guide explains how to create a release package for distribution.

## Quick Method: Using Build Scripts

### Option 1: PowerShell Script (Recommended)

```powershell
# Run from repository root
.\build-release.ps1
```

This will:
- Clean previous builds
- Restore NuGet packages
- Build in Release configuration
- Publish the application
- Create `release-package` folder with all files

### Option 2: Batch File

```cmd
REM Run from repository root
build-release.bat
```

Same functionality as PowerShell script, simpler syntax.

## Manual Method

If you prefer to build manually or need more control:

### Step 1: Clean Build

```powershell
cd WhackerLinkConsoleV234\WhackerLinkConsoleV2
Remove-Item -Recurse -Force bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force obj -ErrorAction SilentlyContinue
```

### Step 2: Restore Packages

```powershell
dotnet restore
```

### Step 3: Build Release

```powershell
dotnet build --configuration Release --no-incremental
```

### Step 4: Publish

```powershell
dotnet publish --configuration Release --output ".\bin\Publish" --self-contained false
```

### Step 5: Create Package Folder

```powershell
cd ..
New-Item -ItemType Directory -Path "release-package" -Force
Copy-Item -Recurse "WhackerLinkConsoleV2\bin\Publish\*" "release-package\"
Copy-Item "RELEASE-NOTES.md" "release-package\"
```

### Step 6: Create ZIP Archive

**Using PowerShell:**
```powershell
$version = git describe --tags --always
Compress-Archive -Path "release-package\*" -DestinationPath "WhackerLinkConsoleV2-$version.zip" -Force
```

**Using 7-Zip:**
```cmd
7z a WhackerLinkConsoleV2-Release.zip .\release-package\*
```

**Using Windows Explorer:**
- Right-click the `release-package` folder
- Select "Send to" → "Compressed (zipped) folder"

## What Gets Included

The release package should contain:

### Essential Files
- `WhackerLinkConsoleV2.exe` - Main executable
- `*.dll` - All dependency libraries
- `WhackerLinkConsoleV2.deps.json` - Dependency manifest
- `WhackerLinkConsoleV2.runtimeconfig.json` - Runtime configuration

### Resource Folders
- `Assets\` - Images and icons
- `Audio\` - Alert tones and sounds
- `codeplugs\` - Sample codeplug files

### Documentation
- `RELEASE-NOTES.md` - Feature documentation
- `README.md` - General documentation (if exists)
- `LICENSE` - License file (if exists)

## File Size Expectations

A typical release package should be:
- **Without .NET Runtime**: ~5-15 MB
- **With Self-Contained .NET**: ~60-80 MB

We recommend **framework-dependent** deployment (smaller size) and letting users install .NET 8.0 Runtime separately.

## Self-Contained Build (Optional)

To create a self-contained package that doesn't require .NET installation:

```powershell
dotnet publish --configuration Release --output ".\bin\Publish-SelfContained" --self-contained true --runtime win-x64
```

This creates a larger package (~60-80 MB) but users don't need to install .NET.

## Testing the Package

Before distributing:

1. **Test on clean machine:**
   - Copy the package to a PC without the development environment
   - Ensure .NET 8.0 Runtime is installed
   - Run WhackerLinkConsoleV2.exe

2. **Verify functionality:**
   - Application starts without errors
   - Can load codeplug
   - PTT Hotkey Settings menu appears
   - Can configure and save hotkey
   - Hotkey works globally
   - Settings persist after restart

3. **Check for missing files:**
   - All audio files present
   - All images/assets load
   - No error messages about missing DLLs

## Creating a GitHub Release

### Step 1: Tag the Release

```bash
git tag -a v1.0.0-ptt-hotkey -m "Add global PTT hotkey feature"
git push origin v1.0.0-ptt-hotkey
```

### Step 2: Create Release on GitHub

1. Go to: https://github.com/miguellini37/WhackerLinkConsoleV234/releases/new
2. Select your tag: `v1.0.0-ptt-hotkey`
3. Release title: `WhackerLink Console V2 - PTT Hotkey Feature`
4. Copy contents from `RELEASE-NOTES.md` into description
5. Upload the ZIP file
6. Check "This is a pre-release" if not final
7. Click "Publish release"

### Step 3: Update Release Branch

```bash
git checkout claude/add-global-ptt-keybind-011CUWSWw3RumU5Epjc91z7Z
git push origin claude/add-global-ptt-keybind-011CUWSWw3RumU5Epjc91z7Z
```

## Distribution Checklist

- [ ] Built in Release configuration
- [ ] Tested on clean machine
- [ ] RELEASE-NOTES.md included
- [ ] Version number updated
- [ ] Git tag created
- [ ] ZIP file created
- [ ] File size reasonable (<20 MB for framework-dependent)
- [ ] All features tested
- [ ] Settings persistence verified
- [ ] Documentation reviewed

## Version Naming Convention

Suggested format: `WhackerLinkConsoleV2-[version]-[feature].zip`

Examples:
- `WhackerLinkConsoleV2-v1.0.0-ptt-hotkey.zip`
- `WhackerLinkConsoleV2-2025.10.26-ptt-feature.zip`
- `WhackerLinkConsoleV2-latest.zip`

## Troubleshooting Build Issues

### "dotnet command not found"
- Install .NET 8.0 SDK from https://dotnet.microsoft.com/download

### "Project file does not exist"
- Ensure you're in the correct directory
- Check that WhackerLinkConsoleV2.csproj exists

### "Restore failed"
- Check internet connection (NuGet packages need to download)
- Clear NuGet cache: `dotnet nuget locals all --clear`

### "Build failed"
- Check for compilation errors in output
- Ensure submodules are initialized: `git submodule update --init --recursive`
- Clean and rebuild

### Missing DLL files in output
- Verify all project references are correct
- Check that submodules are up to date
- Re-run `dotnet restore`

## Support

For packaging issues:
- Check build output for error messages
- Verify .NET SDK version: `dotnet --version` (should be 8.0.x)
- Ensure all submodules are present
- Check disk space (need ~500MB for build artifacts)

---

**Ready to package? Run `build-release.bat` or `build-release.ps1` and you're done!**
