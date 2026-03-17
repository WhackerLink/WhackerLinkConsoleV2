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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WhackerLinkLib.Models.Radio;

namespace WhackerLinkConsoleV2
{
    public partial class KeybindingConfigWindow : Window
    {
        private SettingsManager _settingsManager;
        private ChannelKeybindingManager _channelKeybindingManager;
        private Codeplug _codeplug;
        private string _codeplugIdentifier;
        private MainWindow _mainWindow;

        private bool _recordingGlobalPtt = false;
        private Dictionary<string, TextBox> _channelKeybindingControls = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBox> _channelToggleKeybindingControls = new Dictionary<string, TextBox>();
        
        // Key recording state tracking
        private bool _isRecording = false;
        private ModifierKeys _recordedModifiers = ModifierKeys.None;
        private Key _recordedKey = Key.None;
        private string _pendingKeybinding = "";
        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();

        /// <summary>
        /// Gets whether keybindings were successfully applied and should take immediate effect
        /// </summary>
        public bool KeybindingsApplied { get; private set; } = false;

        public KeybindingConfigWindow(SettingsManager settingsManager, ChannelKeybindingManager channelKeybindingManager, Codeplug codeplug, string codeplugFilePath, MainWindow mainWindow = null)
        {
            InitializeComponent();
            _settingsManager = settingsManager;
            _channelKeybindingManager = channelKeybindingManager;
            _codeplug = codeplug;
            _codeplugIdentifier = ChannelKeybindingManager.GenerateCodeplugIdentifier(codeplugFilePath);
            _mainWindow = mainWindow;

            LoadSettings();
            GenerateChannelControls();
            
            // Suspend all hotkeys while this window is open to prevent background activation
            SuspendMainWindowHotkeys();
        }

        private void LoadSettings()
        {
            // Load global keybinding
            GlobalPttKeybindDisplay.Text = _settingsManager.GlobalPttKeybind;
            
            // Load hotkeys work when unfocused setting
            HotkeysWorkWhenUnfocusedCheckbox.IsChecked = _settingsManager.HotkeysWorkWhenUnfocused;

            // Load codeplug identifier for both tabs
            CodeplugIdentifierText.Text = _codeplugIdentifier;
            CodeplugIdentifierTextToggle.Text = _codeplugIdentifier;
        }

        private void GenerateChannelControls()
        {
            GeneratePttChannelControls();
            GenerateToggleChannelControls();
        }

        private void GeneratePttChannelControls()
        {
            ChannelKeybindingsPanel.Children.Clear();
            _channelKeybindingControls.Clear();

            if (_codeplug == null || _codeplug.Zones == null || _codeplug.Zones.Count == 0)
            {
                var noChannelsText = new TextBlock
                {
                    Text = "No channels available",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                ChannelKeybindingsPanel.Children.Add(noChannelsText);
                return;
            }

            var allChannels = _codeplug.Zones.SelectMany(z => z.Channels).Distinct().OrderBy(c => c.Name).ToList();

            if (allChannels.Count == 0)
            {
                var noChannelsText = new TextBlock
                {
                    Text = "No channels in codeplug",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                ChannelKeybindingsPanel.Children.Add(noChannelsText);
                return;
            }

            foreach (var channel in allChannels)
            {
                var existingKeybind = _channelKeybindingManager.GetChannelKeybinding(_codeplugIdentifier, channel.Name) ?? "";

                var grid = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Channel name
                var channelNameText = new TextBlock
                {
                    Text = channel.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(channelNameText, 0);
                grid.Children.Add(channelNameText);

                // Keybinding textbox
                var keybindTextBox = new TextBox
                {
                    Text = existingKeybind,
                    IsReadOnly = true,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(5),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(keybindTextBox, 1);
                grid.Children.Add(keybindTextBox);

                _channelKeybindingControls[channel.Name] = keybindTextBox;

                // Record button
                var recordButton = new Button
                {
                    Content = "Record",
                    Width = 80,
                    Tag = channel.Name,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                recordButton.Click += (s, e) => RecordChannelKeybind_Click(channel.Name);
                Grid.SetColumn(recordButton, 2);
                grid.Children.Add(recordButton);

                // Clear button
                var clearButton = new Button
                {
                    Content = "Clear",
                    Width = 80,
                    Tag = channel.Name
                };
                clearButton.Click += (s, e) => ClearChannelKeybind_Click(channel.Name);
                Grid.SetColumn(clearButton, 3);
                grid.Children.Add(clearButton);

                ChannelKeybindingsPanel.Children.Add(grid);
            }
        }

        private void GenerateToggleChannelControls()
        {
            ChannelToggleKeybindingsPanel.Children.Clear();
            _channelToggleKeybindingControls.Clear();

            if (_codeplug == null || _codeplug.Zones == null || _codeplug.Zones.Count == 0)
            {
                var noChannelsText = new TextBlock
                {
                    Text = "No channels available",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                ChannelToggleKeybindingsPanel.Children.Add(noChannelsText);
                return;
            }

            var allChannels = _codeplug.Zones.SelectMany(z => z.Channels).Distinct().OrderBy(c => c.Name).ToList();

            if (allChannels.Count == 0)
            {
                var noChannelsText = new TextBlock
                {
                    Text = "No channels in codeplug",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                ChannelToggleKeybindingsPanel.Children.Add(noChannelsText);
                return;
            }

            foreach (var channel in allChannels)
            {
                var existingKeybind = _channelKeybindingManager.GetChannelToggleKeybinding(_codeplugIdentifier, channel.Name) ?? "";

                var grid = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Channel name
                var channelNameText = new TextBlock
                {
                    Text = channel.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(channelNameText, 0);
                grid.Children.Add(channelNameText);

                // Keybinding textbox
                var keybindTextBox = new TextBox
                {
                    Text = existingKeybind,
                    IsReadOnly = true,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(5),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(keybindTextBox, 1);
                grid.Children.Add(keybindTextBox);

                _channelToggleKeybindingControls[channel.Name] = keybindTextBox;

                // Record button
                var recordButton = new Button
                {
                    Content = "Record",
                    Width = 80,
                    Tag = channel.Name,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                recordButton.Click += (s, e) => RecordChannelToggleKeybind_Click(channel.Name);
                Grid.SetColumn(recordButton, 2);
                grid.Children.Add(recordButton);

                // Clear button
                var clearButton = new Button
                {
                    Content = "Clear",
                    Width = 80,
                    Tag = channel.Name
                };
                clearButton.Click += (s, e) => ClearChannelToggleKeybind_Click(channel.Name);
                Grid.SetColumn(clearButton, 3);
                grid.Children.Add(clearButton);

                ChannelToggleKeybindingsPanel.Children.Add(grid);
            }
        }

        private void RecordGlobalPtt_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
            _recordingGlobalPtt = true;
            RecordGlobalPttButton.Content = "Press keys...";
            RecordGlobalPttButton.IsEnabled = false;
            Focus();
        }

        private void RecordChannelKeybind_Click(string channelName)
        {
            if (!_channelKeybindingControls.TryGetValue(channelName, out var textBox))
                return;

            StartRecording();
            var recordingTag = $"recording_ptt_{channelName}";
            Tag = recordingTag;
            textBox.Text = "Press keys...";
            Focus();
        }

        private void RecordChannelToggleKeybind_Click(string channelName)
        {
            if (!_channelToggleKeybindingControls.TryGetValue(channelName, out var textBox))
                return;

            StartRecording();
            var recordingTag = $"recording_toggle_{channelName}";
            Tag = recordingTag;
            textBox.Text = "Press keys...";
            Focus();
        }

        private void ClearChannelKeybind_Click(string channelName)
        {
            if (!_channelKeybindingControls.TryGetValue(channelName, out var textBox))
                return;

            textBox.Text = "";
            _channelKeybindingManager.SetChannelKeybinding(_codeplugIdentifier, channelName, "");
        }

        private void ClearChannelToggleKeybind_Click(string channelName)
        {
            if (!_channelToggleKeybindingControls.TryGetValue(channelName, out var textBox))
                return;

            textBox.Text = "";
            _channelKeybindingManager.SetChannelToggleKeybinding(_codeplugIdentifier, channelName, "");
        }

        private void ClearGlobalPtt_Click(object sender, RoutedEventArgs e)
        {
            GlobalPttKeybindDisplay.Text = "";
            // Reset global PTT recording state if active
            if (_recordingGlobalPtt)
            {
                StopRecording();
                _recordingGlobalPtt = false;
                RecordGlobalPttButton.Content = "Record";
                RecordGlobalPttButton.IsEnabled = true;
            }
        }

        private void StartRecording()
        {
            _isRecording = true;
            _recordedModifiers = ModifierKeys.None;
            _recordedKey = Key.None;
            _pendingKeybinding = "";
            _pressedKeys.Clear();
        }
        
        private void StopRecording()
        {
            _isRecording = false;
            _recordedModifiers = ModifierKeys.None;
            _recordedKey = Key.None;
            _pendingKeybinding = "";
            _pressedKeys.Clear();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!_isRecording)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            var key = e.Key;
            var modifiers = Keyboard.Modifiers;
            
            // Track all pressed keys
            _pressedKeys.Add(key);
            
            // If this is a modifier key, just track it
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin ||
                key == Key.System)
            {
                e.Handled = true;
                UpdateRecordingDisplay(modifiers, Key.None);
                return;
            }
            
            // This is a non-modifier key - capture the combination
            _recordedModifiers = modifiers;
            _recordedKey = key;
            _pendingKeybinding = KeybindingParser.KeybindingToString(modifiers, key);
            
            UpdateRecordingDisplay(modifiers, key);
            e.Handled = true;
        }
        
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (!_isRecording)
            {
                base.OnPreviewKeyUp(e);
                return;
            }
            
            var key = e.Key;
            _pressedKeys.Remove(key);
            
            // If all keys have been released and we have a pending keybinding, finalize it
            if (_pressedKeys.Count == 0 && !string.IsNullOrEmpty(_pendingKeybinding))
            {
                FinalizeRecording();
            }
            
            e.Handled = true;
        }
        
        private void UpdateRecordingDisplay(ModifierKeys modifiers, Key key)
        {
            string displayText;
            if (key == Key.None)
            {
                // Show just the modifiers being held
                displayText = KeybindingParser.ModifiersToString(modifiers);
                if (string.IsNullOrEmpty(displayText))
                {
                    displayText = "Press keys...";
                }
                else
                {
                    displayText += "+...";
                }
            }
            else
            {
                displayText = KeybindingParser.KeybindingToString(modifiers, key);
            }
            
            // Update the appropriate display
            if (_recordingGlobalPtt)
            {
                GlobalPttKeybindDisplay.Text = displayText;
            }
            else if (Tag is string tag && tag.StartsWith("recording_"))
            {
                if (tag.StartsWith("recording_ptt_"))
                {
                    var channelName = tag.Substring("recording_ptt_".Length);
                    if (_channelKeybindingControls.TryGetValue(channelName, out var textBox))
                    {
                        textBox.Text = displayText;
                    }
                }
                else if (tag.StartsWith("recording_toggle_"))
                {
                    var channelName = tag.Substring("recording_toggle_".Length);
                    if (_channelToggleKeybindingControls.TryGetValue(channelName, out var textBox))
                    {
                        textBox.Text = displayText;
                    }
                }
            }
        }
        
        private void FinalizeRecording()
        {
            // Complete the recording with the captured keybinding
            if (_recordingGlobalPtt)
            {
                GlobalPttKeybindDisplay.Text = _pendingKeybinding;
                _recordingGlobalPtt = false;
                RecordGlobalPttButton.Content = "Record";
                RecordGlobalPttButton.IsEnabled = true;
            }
            else if (Tag is string tag && tag.StartsWith("recording_"))
            {
                if (tag.StartsWith("recording_ptt_"))
                {
                    var channelName = tag.Substring("recording_ptt_".Length);
                    if (_channelKeybindingControls.TryGetValue(channelName, out var textBox))
                    {
                        textBox.Text = _pendingKeybinding;
                    }
                }
                else if (tag.StartsWith("recording_toggle_"))
                {
                    var channelName = tag.Substring("recording_toggle_".Length);
                    if (_channelToggleKeybindingControls.TryGetValue(channelName, out var textBox))
                    {
                        textBox.Text = _pendingKeybinding;
                    }
                }
                Tag = null;
            }
            
            StopRecording();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // Validate and save global keybinding
            if (string.IsNullOrWhiteSpace(GlobalPttKeybindDisplay.Text))
            {
                // Clear the global PTT keybinding
                _settingsManager.GlobalPttKeybind = "";
            }
            else
            {
                if (!KeybindingParser.TryParseKeybinding(GlobalPttKeybindDisplay.Text, out _, out _))
                {
                    MessageBox.Show("Invalid Global PTT keybinding format.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                _settingsManager.GlobalPttKeybind = GlobalPttKeybindDisplay.Text;
            }
            
            // Save the hotkeys work when unfocused setting
            _settingsManager.HotkeysWorkWhenUnfocused = HotkeysWorkWhenUnfocusedCheckbox.IsChecked ?? true;

            _settingsManager.SaveSettings();

            // Validate and save per-channel keybindings
            foreach (var kvp in _channelKeybindingControls)
            {
                var channelName = kvp.Key;
                var textBox = kvp.Value;
                var keybinding = textBox.Text;

                if (string.IsNullOrWhiteSpace(keybinding))
                {
                    _channelKeybindingManager.RemoveChannelKeybinding(_codeplugIdentifier, channelName);
                }
                else
                {
                    if (!KeybindingParser.TryParseKeybinding(keybinding, out _, out _))
                    {
                        MessageBox.Show($"Invalid PTT keybinding for channel '{channelName}': {keybinding}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    _channelKeybindingManager.SetChannelKeybinding(_codeplugIdentifier, channelName, keybinding);
                }
            }

            // Validate and save per-channel toggle keybindings
            foreach (var kvp in _channelToggleKeybindingControls)
            {
                var channelName = kvp.Key;
                var textBox = kvp.Value;
                var keybinding = textBox.Text;

                if (string.IsNullOrWhiteSpace(keybinding))
                {
                    _channelKeybindingManager.RemoveChannelToggleKeybinding(_codeplugIdentifier, channelName);
                }
                else
                {
                    if (!KeybindingParser.TryParseKeybinding(keybinding, out _, out _))
                    {
                        MessageBox.Show($"Invalid toggle keybinding for channel '{channelName}': {keybinding}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    _channelKeybindingManager.SetChannelToggleKeybinding(_codeplugIdentifier, channelName, keybinding);
                }
            }

            KeybindingsApplied = true;

            MessageBox.Show("Keybindings saved and applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SuspendMainWindowHotkeys()
        {
            try
            {
                _mainWindow?.SuspendHotkeys();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Failed to suspend main window hotkeys: {ex.Message}");
            }
        }

        private void ResumeMainWindowHotkeys()
        {
            try
            {
                _mainWindow?.ResumeHotkeys();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Failed to resume main window hotkeys: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Always resume hotkeys when the window is closed, regardless of how it was closed
            ResumeMainWindowHotkeys();
            base.OnClosed(e);
        }
    }
}
