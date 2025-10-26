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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WhackerLinkConsoleV2
{
    public class GlobalHotkeyManager
    {
        // Windows API Constants
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        // P/Invoke declarations
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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
        private IntPtr _windowHandle;
        private HwndSource _source;
        private bool _isRegistered;

        public event EventHandler HotkeyPressed;

        public GlobalHotkeyManager(Window window)
        {
            _window = window;
        }

        public void Initialize()
        {
            // Get window handle
            WindowInteropHelper helper = new WindowInteropHelper(_window);
            _windowHandle = helper.Handle;

            // Hook into the window's message processing
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(WndProc);
        }

        public bool RegisterHotkey(KeyModifier modifiers, System.Windows.Forms.Keys key)
        {
            if (_isRegistered)
                UnregisterHotkey();

            try
            {
                _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)modifiers, (uint)key);
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
            if (_isRegistered)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotkey();
            _source?.RemoveHook(WndProc);
        }
    }
}
