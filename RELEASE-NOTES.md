# WhackerLink Console V2 - Release Notes

## New Features: Global PTT Hotkey

This release adds a **global push-to-talk hotkey system** that allows dispatchers to activate PTT without needing to focus on the application window.

### What's New

#### 1. Global PTT Hotkey System
- Configure a system-wide keyboard shortcut for push-to-talk
- Works from any application - no need to click back to WhackerLink
- Hold-to-talk behavior: press and hold to transmit, release to stop
- Settings persist across application restarts

#### 2. PTT Hotkey Settings Window
- New menu item: **Edit → PTT Hotkey Settings**
- Easy-to-use interface for configuring hotkeys
- Support for modifier keys: Ctrl, Alt, Shift, Win
- Real-time key capture for intuitive configuration

#### 3. Hold-to-Talk Behavior
- Press and hold your configured hotkey to start transmitting
- Release the key to stop transmitting
- Behaves like a physical push-to-talk radio button
- UI button still works as toggle for mouse users

### Installation

#### Requirements
- Windows 7 or later (Windows 10/11 recommended)
- .NET 8.0 Runtime (included in package or download from microsoft.com)
- Audio input/output devices

#### Quick Start
1. Extract all files from the ZIP to a folder of your choice
2. Run `WhackerLinkConsoleV2.exe`
3. Load your codeplug (File → Open Codeplug)
4. Configure your PTT hotkey (Edit → PTT Hotkey Settings)
5. Select channels and start using!

### Configuration Guide

#### Setting Up PTT Hotkey

1. **Open Settings**
   - Click **Edit** → **PTT Hotkey Settings** in the menu

2. **Enable Hotkey**
   - Check **"Enable Global PTT Hotkey"**

3. **Choose Modifier Keys**
   - Select one or more: Ctrl, Alt, Shift, Win
   - Example: Ctrl + Alt

4. **Select Main Key**
   - Click in the **Key** field
   - Press your desired key (F1-F12, letters, numbers, etc.)
   - Example: F1

5. **Save**
   - Click **Save**
   - Your hotkey is now active!

#### Recommended Hotkey Combinations
- `Ctrl + Alt + F1` - Easy to reach, won't conflict with most apps
- `Ctrl + Shift + Space` - Natural for push-to-talk
- `Win + F1` - Rarely used by other applications
- `Ctrl + Alt + T` - Mnemonic for "Talk"

**Avoid These:**
- Single keys without modifiers (will interfere with typing)
- Common shortcuts like Ctrl+C, Ctrl+V, Alt+Tab
- Function keys F1-F12 alone (used by many applications)

### Usage

#### Using the Hotkey
1. Select one or more channels for transmission
2. **Press and hold** your configured hotkey
3. Speak into your microphone
4. **Release the key** when done
5. Works even when WhackerLink is in the background!

#### Using the Button
- Click the Global PTT button to toggle on
- Click again to toggle off
- Visual indicator: Red = transmitting, Gray = idle

### Features Summary

| Feature | Description |
|---------|-------------|
| Global Hotkey | System-wide keyboard shortcut for PTT |
| Hold-to-Talk | Press to talk, release to stop |
| Configurable Keys | Support for Ctrl, Alt, Shift, Win modifiers |
| Settings Persistence | Configuration saved to UserSettings.json |
| Background Operation | Works when app is not focused |
| Visual Feedback | Button color indicates PTT status |

### Technical Details

#### Settings File Location
Your PTT hotkey configuration is saved in:
```
<Application Directory>\UserSettings.json
```

#### Settings File Format
```json
{
  "EnableGlobalPttHotkey": true,
  "PttHotkeyModifiers": 3,
  "PttHotkeyKey": 112
}
```

Where:
- `EnableGlobalPttHotkey`: true/false
- `PttHotkeyModifiers`: Bitmask (1=Alt, 2=Ctrl, 4=Shift, 8=Win)
- `PttHotkeyKey`: Virtual key code (e.g., 112 = F1)

### Troubleshooting

#### Hotkey Not Working
1. **Check if enabled**: Open PTT Hotkey Settings, ensure checkbox is checked
2. **Key conflict**: Try a different key combination
3. **Permission issue**: Run as administrator if needed
4. **Restart app**: Close and reopen WhackerLinkConsoleV2

#### Hotkey Registration Failed
- The hotkey may be in use by another application
- Try a different key combination
- Common conflicts: Gaming software, system utilities

#### Settings Not Saving
- Check write permissions on application directory
- Ensure UserSettings.json is not read-only
- Check for error messages in the console window

### Changelog

#### Version (Current Branch: claude/add-global-ptt-keybind-011CUWSWw3RumU5Epjc91z7Z)

**New Features:**
- Added global PTT hotkey system with hold-to-talk behavior
- Added PTT Hotkey Settings window for easy configuration
- Added support for modifier keys (Ctrl, Alt, Shift, Win)
- Added settings persistence for hotkey configuration

**Improvements:**
- Refactored PTT logic for consistency between button and hotkey
- Added visual feedback for hotkey activation
- Improved state management to prevent duplicate PTT operations

**Technical:**
- Implemented low-level keyboard hook for global key detection
- Added key press and release event handling
- Created GlobalHotkeyManager class for hotkey management
- Extended SettingsManager with hotkey configuration properties

### License

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

### Credits

- Original WhackerLink Console by Caleb, K4PHP
- PTT Hotkey Feature by Claude Code

### Support

For issues, questions, or feature requests:
- GitHub Issues: https://github.com/miguellini37/WhackerLinkConsoleV234/issues
- Check UserSettings.json for configuration issues
- Review console output for error messages

### Building from Source

```bash
git clone https://github.com/miguellini37/WhackerLinkConsoleV234.git
cd WhackerLinkConsoleV234
git checkout claude/add-global-ptt-keybind-011CUWSWw3RumU5Epjc91z7Z
git submodule update --init --recursive
cd WhackerLinkConsoleV2
dotnet restore
dotnet build --configuration Release
```

---

**Enjoy your new global PTT hotkey feature!**
