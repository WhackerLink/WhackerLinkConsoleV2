/*
* WhackerLink - WhackerLinkConsoleV2
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
* 
* Copyright (C) 2024-2025 Caleb, K4PHP
* Copyright (C) 2025 Firav (firavdev@gmail.com)
* 
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace WhackerLinkConsoleV2
{
    /// <summary>
    /// Represents arguments for hotkey events
    /// </summary>
    public class HotKeyEventArgs : EventArgs
    {
        public int HotKeyId { get; set; }
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
    }

    /// <summary>
    /// Manages global system hotkeys using low-level keyboard hooks
    /// </summary>
    public class GlobalHotKeyManager : IDisposable
    {
        // Windows API P/Invoke declarations for keyboard hooks
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // Hook constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        // Virtual key codes for modifier keys
        private const int VK_CONTROL = 0x11;
        private const int VK_SHIFT = 0x10;
        private const int VK_MENU = 0x12; // Alt key

        // Delegate for low-level keyboard hook
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private Window _window;
        private IntPtr _windowHandle;
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _hookCallback;
        private Dictionary<int, HotKeyInfo> _registeredHotKeys = new Dictionary<int, HotKeyInfo>();
        private Dictionary<int, bool> _hotKeyStates = new Dictionary<int, bool>(); // Track if hotkey is currently pressed
        private Dictionary<int, int> _vkCodeToHotKeyId = new Dictionary<int, int>(); // Map VK code to the currently active hotkey ID for that key
        private int _nextHotKeyId = 1;
        private HashSet<int> _currentlyPressedVKs = new HashSet<int>(); // Track currently pressed virtual keys
        
        /// <summary>
        /// Gets or sets whether hotkeys should work when the application is not focused
        /// </summary>
        public bool WorkWhenUnfocused { get; set; } = true;

        public event EventHandler<HotKeyEventArgs> HotKeyPressed;
        public event EventHandler<HotKeyEventArgs> HotKeyReleased;

        /// <summary>
        /// Internal class to track hotkey information
        /// </summary>
        private class HotKeyInfo
        {
            public int Id { get; set; }
            public Key Key { get; set; }
            public ModifierKeys Modifiers { get; set; }
            public Action OnKeyDown { get; set; }
            public Action OnKeyUp { get; set; }
        }

        /// <summary>
        /// Initializes the global hotkey manager for the specified window
        /// </summary>
        public void Initialize(Window window)
        {
            _window = window;
            _windowHandle = new WindowInteropHelper(window).Handle;
            
            if (_windowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Window handle is invalid. Ensure the window is fully initialized.");
            }

            // Set up the low-level keyboard hook
            _hookCallback = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(curModule.ModuleName), 0);
            }

            if (_hookId == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to install keyboard hook.");
            }

            Console.WriteLine("Low-level keyboard hook installed successfully.");
        }

        /// <summary>
        /// Registers a global hotkey
        /// </summary>
        /// <returns>Returns the hotkey ID if successful, -1 if failed</returns>
        public int RegisterHotKey(ModifierKeys modifier, Key key, Action onKeyDown = null, Action onKeyUp = null)
        {
            if (_hookId == IntPtr.Zero)
            {
                Console.WriteLine($"ERROR: GlobalHotKeyManager not initialized. Call Initialize() first.");
                return -1;
            }

            int hotKeyId = _nextHotKeyId++;
            uint vkCode = (uint)KeyInterop.VirtualKeyFromKey(key);

            if (vkCode == 0)
            {
                Console.WriteLine($"ERROR: Could not convert key {key} to virtual key code.");
                return -1;
            }

            var hotKeyInfo = new HotKeyInfo
            {
                Id = hotKeyId,
                Key = key,
                Modifiers = modifier,
                OnKeyDown = onKeyDown,
                OnKeyUp = onKeyUp
            };

            _registeredHotKeys[hotKeyId] = hotKeyInfo;
            
            string modifierStr = modifier == ModifierKeys.None ? "(no modifiers)" : modifier.ToString();
            Console.WriteLine($"Registered hotkey: {modifierStr}+{key} (ID: {hotKeyId})");
            
            return hotKeyId;
        }

        /// <summary>
        /// Unregisters a global hotkey by ID
        /// </summary>
        public bool UnregisterHotKey(int hotKeyId)
        {
            if (!_registeredHotKeys.ContainsKey(hotKeyId))
            {
                Console.WriteLine($"WARNING: Hotkey ID {hotKeyId} not found in registered hotkeys.");
                return false;
            }

            _registeredHotKeys.Remove(hotKeyId);
            _hotKeyStates.Remove(hotKeyId);
            Console.WriteLine($"Unregistered hotkey ID {hotKeyId}");
            return true;
        }

        /// <summary>
        /// Unregisters all registered hotkeys
        /// </summary>
        public void UnregisterAllHotKeys()
        {
            var hotKeyIds = new List<int>(_registeredHotKeys.Keys);
            foreach (var id in hotKeyIds)
            {
                UnregisterHotKey(id);
            }
        }

        /// <summary>
        /// Gets information about a registered hotkey
        /// </summary>
        public bool GetHotKeyInfo(int hotKeyId, out ModifierKeys modifier, out Key key)
        {
            modifier = ModifierKeys.None;
            key = Key.None;

            if (_registeredHotKeys.TryGetValue(hotKeyId, out var hotKeyInfo))
            {
                modifier = hotKeyInfo.Modifiers;
                key = hotKeyInfo.Key;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the list of all registered hotkey IDs
        /// </summary>
        public IEnumerable<int> GetRegisteredHotKeyIds()
        {
            return new List<int>(_registeredHotKeys.Keys);
        }

        /// <summary>
        /// Low-level keyboard hook callback
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                bool isKeyDown = (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN);
                bool isKeyUp = (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP);

                if (isKeyDown || isKeyUp)
                {
                    KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    int vkCode = (int)hookStruct.vkCode;

                    // Track currently pressed keys
                    if (isKeyDown)
                    {
                        _currentlyPressedVKs.Add(vkCode);
                    }
                    else if (isKeyUp)
                    {
                        _currentlyPressedVKs.Remove(vkCode);
                    }

                    // Check if we should process hotkeys (based on focus setting)
                    if (!WorkWhenUnfocused)
                    {
                        IntPtr foregroundWindow = GetForegroundWindow();
                        if (foregroundWindow != _windowHandle)
                        {
                            // App is not focused and WorkWhenUnfocused is false, so ignore
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }
                    }

                    if (isKeyDown)
                    {
                        // Get current modifier state
                        ModifierKeys currentModifiers = GetCurrentModifiers();

                        // Check all registered hotkeys for a match
                        foreach (var kvp in _registeredHotKeys)
                        {
                            var hotKeyInfo = kvp.Value;
                            int hotKeyId = kvp.Key;

                            // Check if this hotkey matches the current key + modifiers
                            int hotKeyVK = KeyInterop.VirtualKeyFromKey(hotKeyInfo.Key);
                            
                            if (hotKeyVK == vkCode && currentModifiers == hotKeyInfo.Modifiers)
                            {
                                // Check if already pressed to prevent repeats
                                bool wasAlreadyPressed = _hotKeyStates.TryGetValue(hotKeyId, out bool isPressed) && isPressed;
                                
                                if (!wasAlreadyPressed)
                                {
                                    _hotKeyStates[hotKeyId] = true;
                                    _vkCodeToHotKeyId[vkCode] = hotKeyId; // Track which hotkey this VK is associated with

                                    // Dispatch to UI thread
                                    _window?.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        var args = new HotKeyEventArgs
                                        {
                                            HotKeyId = hotKeyId,
                                            Key = hotKeyInfo.Key,
                                            Modifiers = hotKeyInfo.Modifiers
                                        };

                                        HotKeyPressed?.Invoke(this, args);
                                        hotKeyInfo.OnKeyDown?.Invoke();
                                    }));
                                }
                                break; // Found a match, no need to check other hotkeys
                            }
                        }
                    }
                    else if (isKeyUp)
                    {
                        // Check if this VK code was associated with a hotkey press
                        if (_vkCodeToHotKeyId.TryGetValue(vkCode, out int hotKeyId))
                        {
                            // Mark as released
                            if (_hotKeyStates.ContainsKey(hotKeyId) && _hotKeyStates[hotKeyId])
                            {
                                _hotKeyStates[hotKeyId] = false;
                                _vkCodeToHotKeyId.Remove(vkCode);

                                if (_registeredHotKeys.TryGetValue(hotKeyId, out var hotKeyInfo))
                                {
                                    // Dispatch to UI thread
                                    _window?.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        var args = new HotKeyEventArgs
                                        {
                                            HotKeyId = hotKeyId,
                                            Key = hotKeyInfo.Key,
                                            Modifiers = hotKeyInfo.Modifiers
                                        };

                                        HotKeyReleased?.Invoke(this, args);
                                        hotKeyInfo.OnKeyUp?.Invoke();
                                    }));
                                }
                            }
                        }
                    }
                }
            }

            // Always call the next hook - this ensures keys work in other applications
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// Gets the current modifier keys state
        /// </summary>
        private ModifierKeys GetCurrentModifiers()
        {
            ModifierKeys modifiers = ModifierKeys.None;

            if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
                modifiers |= ModifierKeys.Control;
            if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
                modifiers |= ModifierKeys.Shift;
            if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
                modifiers |= ModifierKeys.Alt;
            if ((GetAsyncKeyState((int)Key.LWin) & 0x8000) != 0 || (GetAsyncKeyState((int)Key.RWin) & 0x8000) != 0)
                modifiers |= ModifierKeys.Windows;

            return modifiers;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            // Unhook the keyboard hook
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                Console.WriteLine("Keyboard hook uninstalled.");
            }

            UnregisterAllHotKeys();

            _window = null;
            _windowHandle = IntPtr.Zero;
            _currentlyPressedVKs.Clear();
            _vkCodeToHotKeyId.Clear();
        }
    }
}