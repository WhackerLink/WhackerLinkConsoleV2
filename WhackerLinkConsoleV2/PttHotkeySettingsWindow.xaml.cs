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
using System.Windows;
using System.Windows.Input;

namespace WhackerLinkConsoleV2
{
    public partial class PttHotkeySettingsWindow : Window
    {
        private readonly SettingsManager _settingsManager;
        private readonly GlobalHotkeyManager _hotkeyManager;
        private int _selectedKey = 0;

        public PttHotkeySettingsWindow(SettingsManager settingsManager, GlobalHotkeyManager hotkeyManager)
        {
            InitializeComponent();
            _settingsManager = settingsManager;
            _hotkeyManager = hotkeyManager;

            LoadSettings();
        }

        private void LoadSettings()
        {
            EnableHotkeyCheckBox.IsChecked = _settingsManager.EnableGlobalPttHotkey;

            if (_settingsManager.PttHotkeyModifiers != 0 || _settingsManager.PttHotkeyKey != 0)
            {
                var modifiers = (GlobalHotkeyManager.KeyModifier)_settingsManager.PttHotkeyModifiers;

                CtrlCheckBox.IsChecked = (modifiers & GlobalHotkeyManager.KeyModifier.Control) != 0;
                AltCheckBox.IsChecked = (modifiers & GlobalHotkeyManager.KeyModifier.Alt) != 0;
                ShiftCheckBox.IsChecked = (modifiers & GlobalHotkeyManager.KeyModifier.Shift) != 0;
                WinCheckBox.IsChecked = (modifiers & GlobalHotkeyManager.KeyModifier.Win) != 0;

                _selectedKey = _settingsManager.PttHotkeyKey;
                if (_selectedKey != 0)
                {
                    HotkeyTextBox.Text = ((System.Windows.Forms.Keys)_selectedKey).ToString();
                }
            }
        }

        private void EnableHotkeyCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // The IsEnabled binding will handle disabling/enabling the controls
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            // Convert WPF key to Windows Forms key
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Ignore modifier keys by themselves
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            try
            {
                // Convert to virtual key code
                _selectedKey = KeyInterop.VirtualKeyFromKey(key);
                HotkeyTextBox.Text = key.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Build modifier flags
                GlobalHotkeyManager.KeyModifier modifiers = GlobalHotkeyManager.KeyModifier.None;
                if (CtrlCheckBox.IsChecked == true)
                    modifiers |= GlobalHotkeyManager.KeyModifier.Control;
                if (AltCheckBox.IsChecked == true)
                    modifiers |= GlobalHotkeyManager.KeyModifier.Alt;
                if (ShiftCheckBox.IsChecked == true)
                    modifiers |= GlobalHotkeyManager.KeyModifier.Shift;
                if (WinCheckBox.IsChecked == true)
                    modifiers |= GlobalHotkeyManager.KeyModifier.Win;

                bool enableHotkey = EnableHotkeyCheckBox.IsChecked == true;

                // Validate that a key is selected if hotkey is enabled
                if (enableHotkey && _selectedKey == 0)
                {
                    MessageBox.Show("Please select a key for the hotkey.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Unregister old hotkey
                _hotkeyManager.UnregisterHotkey();

                // Save settings
                _settingsManager.EnableGlobalPttHotkey = enableHotkey;
                _settingsManager.PttHotkeyModifiers = (int)modifiers;
                _settingsManager.PttHotkeyKey = _selectedKey;
                _settingsManager.SaveSettings();

                // Register new hotkey if enabled
                if (enableHotkey && _selectedKey != 0)
                {
                    var key = (System.Windows.Forms.Keys)_selectedKey;
                    bool registered = _hotkeyManager.RegisterHotkey(modifiers, key);

                    if (!registered)
                    {
                        MessageBox.Show("Failed to register hotkey. The hotkey may already be in use by another application.",
                            "Hotkey Registration Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _settingsManager.EnableGlobalPttHotkey = false;
                        _settingsManager.SaveSettings();
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving hotkey settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
