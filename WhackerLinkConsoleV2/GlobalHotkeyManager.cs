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
*
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace WhackerLinkConsoleV2
{
    public class GlobalHotkeyManager
    {
        // Windows API Constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        // P/Invoke declarations
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

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        // Modifier key flags
        public enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8
        }

        private readonly Window _window;
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private bool _isRegistered;

        private KeyModifier _modifiers = KeyModifier.None;
        private System.Windows.Forms.Keys _key = System.Windows.Forms.Keys.None;
        private bool _isHotkeyCurrentlyPressed = false;

        public event EventHandler HotkeyPressed;
        public event EventHandler HotkeyReleased;

        public GlobalHotkeyManager(Window window)
        {
            _window = window;
            _proc = HookCallback; // Keep a reference to prevent garbage collection
        }

        public void Initialize()
        {
            // No initialization needed for keyboard hook approach
        }

        public bool RegisterHotkey(KeyModifier modifiers, System.Windows.Forms.Keys key)
        {
            if (_isRegistered)
                UnregisterHotkey();

            try
            {
                _modifiers = modifiers;
                _key = key;

                // Install the keyboard hook
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }

                _isRegistered = _hookID != IntPtr.Zero;
                return _isRegistered;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register hotkey: {ex.Message}");
                return false;
            }
        }

        public void UnregisterHotkey()
        {
            if (_isRegistered && _hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                _isRegistered = false;
                _isHotkeyCurrentlyPressed = false;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isKeyDown = (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN);
                bool isKeyUp = (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP);

                // Check if this is our configured key
                if (vkCode == (int)_key)
                {
                    if (isKeyDown && !_isHotkeyCurrentlyPressed)
                    {
                        // Check if all required modifiers are pressed
                        if (AreModifiersPressed(_modifiers))
                        {
                            _isHotkeyCurrentlyPressed = true;
                            _window.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                            }));
                        }
                    }
                    else if (isKeyUp && _isHotkeyCurrentlyPressed)
                    {
                        _isHotkeyCurrentlyPressed = false;
                        _window.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            HotkeyReleased?.Invoke(this, EventArgs.Empty);
                        }));
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool AreModifiersPressed(KeyModifier modifiers)
        {
            bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
            bool altPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU (Alt)
            bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
            bool winPressed = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0; // VK_LWIN or VK_RWIN

            bool ctrlNeeded = (modifiers & KeyModifier.Control) != 0;
            bool altNeeded = (modifiers & KeyModifier.Alt) != 0;
            bool shiftNeeded = (modifiers & KeyModifier.Shift) != 0;
            bool winNeeded = (modifiers & KeyModifier.Win) != 0;

            // All required modifiers must be pressed, and no unrequired modifiers should be pressed
            return (ctrlNeeded == ctrlPressed) &&
                   (altNeeded == altPressed) &&
                   (shiftNeeded == shiftPressed) &&
                   (winNeeded == winPressed);
        }

        public void Dispose()
        {
            UnregisterHotkey();
        }
    }
}
