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
using System.Windows.Input;
using WhackerLinkConsoleV2.Controls;

namespace WhackerLinkConsoleV2
{
    /// <summary>
    /// Manages per-channel PTT and toggle hotkey registrations and triggering
    /// </summary>
    public class ChannelHotKeyManager
    {
        private GlobalHotKeyManager _globalHotKeyManager;
        private ChannelKeybindingManager _channelKeybindingManager;
        private Dictionary<string, int> _channelHotKeyIds = new Dictionary<string, int>();
        private Dictionary<string, int> _channelToggleHotKeyIds = new Dictionary<string, int>();
        private Dictionary<int, string> _hotKeyIdToChannelName = new Dictionary<int, string>();
        private Dictionary<int, string> _toggleHotKeyIdToChannelName = new Dictionary<int, string>();
        private List<ChannelBox> _allChannels = new List<ChannelBox>();
        private string _currentCodeplugIdentifier;
        private Action<ChannelBox, bool> _onChannelPttTriggered;
        private Action<ChannelBox> _onChannelToggleTriggered;

        public ChannelHotKeyManager(GlobalHotKeyManager globalHotKeyManager, ChannelKeybindingManager channelKeybindingManager, Action<ChannelBox, bool> onChannelPttTriggered, Action<ChannelBox> onChannelToggleTriggered = null)
        {
            _globalHotKeyManager = globalHotKeyManager;
            _channelKeybindingManager = channelKeybindingManager;
            _onChannelPttTriggered = onChannelPttTriggered;
            _onChannelToggleTriggered = onChannelToggleTriggered;
        }

        /// <summary>
        /// Initializes channel hotkeys for the current codeplug
        /// </summary>
        public void InitializeChannelHotkeys(string codeplugIdentifier, List<ChannelBox> channels)
        {
            // Always unregister all existing channel hotkeys first to ensure 
            // cleared keybindings are properly removed
            UnregisterAllChannelHotkeys();
            
            // Update current codeplug identifier
            _currentCodeplugIdentifier = codeplugIdentifier;
            _allChannels = channels;

            // Register PTT hotkeys for channels that have PTT keybindings
            int registeredPttCount = 0;
            int registeredToggleCount = 0;
            
            foreach (var channel in channels)
            {
                // Register PTT hotkey
                var pttKeybinding = _channelKeybindingManager.GetChannelKeybinding(codeplugIdentifier, channel.ChannelName);
                if (!string.IsNullOrWhiteSpace(pttKeybinding))
                {
                    RegisterChannelHotkey(channel.ChannelName, pttKeybinding);
                    registeredPttCount++;
                }

                // Register toggle hotkey
                var toggleKeybinding = _channelKeybindingManager.GetChannelToggleKeybinding(codeplugIdentifier, channel.ChannelName);
                if (!string.IsNullOrWhiteSpace(toggleKeybinding))
                {
                    RegisterChannelToggleHotkey(channel.ChannelName, toggleKeybinding);
                    registeredToggleCount++;
                }
            }
            
            if (registeredPttCount > 0)
            {
                Console.WriteLine($"Registered {registeredPttCount} channel PTT hotkey(s)");
            }
            if (registeredToggleCount > 0)
            {
                Console.WriteLine($"Registered {registeredToggleCount} channel toggle hotkey(s)");
            }
        }

        /// <summary>
        /// Registers a hotkey for a specific channel
        /// </summary>
        private void RegisterChannelHotkey(string channelName, string keybinding)
        {
            if (_globalHotKeyManager == null)
            {
                Console.WriteLine($"ERROR: GlobalHotKeyManager is null! Cannot register hotkey for '{channelName}'");
                return;
            }

            // Unregister old hotkey if it exists
            if (_channelHotKeyIds.ContainsKey(channelName))
            {
                var oldHotKeyId = _channelHotKeyIds[channelName];
                _globalHotKeyManager.UnregisterHotKey(oldHotKeyId);
                _channelHotKeyIds.Remove(channelName);
                _hotKeyIdToChannelName.Remove(oldHotKeyId);
            }

            // Parse the keybinding
            if (!KeybindingParser.TryParseKeybinding(keybinding, out var modifiers, out var key))
            {
                Console.WriteLine($"ERROR: Failed to parse keybinding '{keybinding}' for channel '{channelName}'");
                return;
            }

            // Register with global hotkey manager
            var hotKeyId = _globalHotKeyManager.RegisterHotKey(
                modifiers,
                key,
                () => OnChannelHotKeyDown(channelName),
                () => OnChannelHotKeyUp(channelName)
            );

            if (hotKeyId != -1)
            {
                _channelHotKeyIds[channelName] = hotKeyId;
                _hotKeyIdToChannelName[hotKeyId] = channelName;
            }
            else
            {
                Console.WriteLine($"Failed to register hotkey '{keybinding}' for channel '{channelName}'");
            }
        }

        /// <summary>
        /// Registers a toggle hotkey for a specific channel
        /// </summary>
        private void RegisterChannelToggleHotkey(string channelName, string keybinding)
        {
            if (_globalHotKeyManager == null)
            {
                Console.WriteLine($"ERROR: GlobalHotKeyManager is null! Cannot register toggle hotkey for '{channelName}'");
                return;
            }

            // Unregister old toggle hotkey if it exists
            if (_channelToggleHotKeyIds.ContainsKey(channelName))
            {
                var oldHotKeyId = _channelToggleHotKeyIds[channelName];
                _globalHotKeyManager.UnregisterHotKey(oldHotKeyId);
                _channelToggleHotKeyIds.Remove(channelName);
                _toggleHotKeyIdToChannelName.Remove(oldHotKeyId);
            }

            // Parse the keybinding
            if (!KeybindingParser.TryParseKeybinding(keybinding, out var modifiers, out var key))
            {
                Console.WriteLine($"ERROR: Failed to parse toggle keybinding '{keybinding}' for channel '{channelName}'");
                return;
            }

            // Register with global hotkey manager (toggle only needs key down event)
            var hotKeyId = _globalHotKeyManager.RegisterHotKey(
                modifiers,
                key,
                () => OnChannelToggleHotKeyDown(channelName),
                null // No key up action needed for toggle
            );

            if (hotKeyId != -1)
            {
                _channelToggleHotKeyIds[channelName] = hotKeyId;
                _toggleHotKeyIdToChannelName[hotKeyId] = channelName;
            }
            else
            {
                Console.WriteLine($"Failed to register toggle hotkey '{keybinding}' for channel '{channelName}'");
            }
        }

        /// <summary>
        /// Unregisters a hotkey for a specific channel
        /// </summary>
        public void UnregisterChannelHotkey(string channelName)
        {
            // Unregister PTT hotkey
            if (_channelHotKeyIds.TryGetValue(channelName, out var pttHotKeyId))
            {
                _globalHotKeyManager.UnregisterHotKey(pttHotKeyId);
                _channelHotKeyIds.Remove(channelName);
                _hotKeyIdToChannelName.Remove(pttHotKeyId);
            }

            // Unregister toggle hotkey
            if (_channelToggleHotKeyIds.TryGetValue(channelName, out var toggleHotKeyId))
            {
                _globalHotKeyManager.UnregisterHotKey(toggleHotKeyId);
                _channelToggleHotKeyIds.Remove(channelName);
                _toggleHotKeyIdToChannelName.Remove(toggleHotKeyId);
            }
        }

        /// <summary>
        /// Unregisters all channel hotkeys
        /// </summary>
        public void UnregisterAllChannelHotkeys()
        {
            // Unregister all PTT hotkeys
            var channelNames = new List<string>(_channelHotKeyIds.Keys);
            foreach (var channelName in channelNames)
            {
                if (_channelHotKeyIds.TryGetValue(channelName, out var hotKeyId))
                {
                    _globalHotKeyManager.UnregisterHotKey(hotKeyId);
                    _channelHotKeyIds.Remove(channelName);
                    _hotKeyIdToChannelName.Remove(hotKeyId);
                }
            }

            // Unregister all toggle hotkeys
            var toggleChannelNames = new List<string>(_channelToggleHotKeyIds.Keys);
            foreach (var channelName in toggleChannelNames)
            {
                if (_channelToggleHotKeyIds.TryGetValue(channelName, out var hotKeyId))
                {
                    _globalHotKeyManager.UnregisterHotKey(hotKeyId);
                    _channelToggleHotKeyIds.Remove(channelName);
                    _toggleHotKeyIdToChannelName.Remove(hotKeyId);
                }
            }
        }

        /// <summary>
        /// Called when a channel hotkey is pressed
        /// </summary>
        private void OnChannelHotKeyDown(string channelName)
        {
            var channel = _allChannels.FirstOrDefault(c => c.ChannelName == channelName);
            if (channel != null)
            {
                _onChannelPttTriggered?.Invoke(channel, true);
            }
        }

        /// <summary>
        /// Called when a channel hotkey is released
        /// </summary>
        private void OnChannelHotKeyUp(string channelName)
        {
            var channel = _allChannels.FirstOrDefault(c => c.ChannelName == channelName);
            if (channel != null)
            {
                _onChannelPttTriggered?.Invoke(channel, false);
            }
        }

        /// <summary>
        /// Called when a channel toggle hotkey is pressed
        /// </summary>
        private void OnChannelToggleHotKeyDown(string channelName)
        {
            var channel = _allChannels.FirstOrDefault(c => c.ChannelName == channelName);
            if (channel != null)
            {
                _onChannelToggleTriggered?.Invoke(channel);
            }
        }
    }
}
