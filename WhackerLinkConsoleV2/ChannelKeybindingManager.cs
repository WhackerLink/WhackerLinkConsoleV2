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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace WhackerLinkConsoleV2
{
    /// <summary>
    /// Manages per-channel PTT keybindings, storing them persistently.
    /// Bindings are organized by codeplug + channel name for dynamic channel support.
    /// </summary>
    public class ChannelKeybindingManager
    {
        private const string ChannelKeybindingsFilePath = "ChannelKeybindings.json";

        private Dictionary<string, ChannelKeybindingProfile> _keybindingProfiles = 
            new Dictionary<string, ChannelKeybindingProfile>();

        /// <summary>
        /// Represents keybindings for a specific codeplug
        /// </summary>
        public class ChannelKeybindingProfile
        {
            /// <summary>
            /// Channel name -> PTT keybinding string mapping (e.g., "MainChannel" -> "Ctrl+T")
            /// </summary>
            public Dictionary<string, string> ChannelKeybindings { get; set; } = 
                new Dictionary<string, string>();
                
            /// <summary>
            /// Channel name -> toggle keybinding string mapping (e.g., "MainChannel" -> "Ctrl+G")
            /// </summary>
            public Dictionary<string, string> ChannelToggleKeybindings { get; set; } = 
                new Dictionary<string, string>();
        }

        /// <summary>
        /// Loads keybinding profiles from disk
        /// </summary>
        public void LoadKeybindings()
        {
            if (!File.Exists(ChannelKeybindingsFilePath))
            {
                Console.WriteLine("Channel keybindings file not found. Starting with empty configuration.");
                return;
            }

            try
            {
                var json = File.ReadAllText(ChannelKeybindingsFilePath);
                var loadedProfiles = JsonConvert.DeserializeObject<Dictionary<string, ChannelKeybindingProfile>>(json);

                if (loadedProfiles != null)
                {
                    _keybindingProfiles = loadedProfiles;
                    Console.WriteLine($"Loaded {_keybindingProfiles.Count} channel keybinding profiles.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to load channel keybindings: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves all keybinding profiles to disk
        /// </summary>
        public void SaveKeybindings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_keybindingProfiles, Formatting.Indented);
                File.WriteAllText(ChannelKeybindingsFilePath, json);
                Console.WriteLine("Channel keybindings saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to save channel keybindings: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets or creates a keybinding profile for a codeplug
        /// </summary>
        private ChannelKeybindingProfile GetOrCreateProfile(string codeplugIdentifier)
        {
            if (!_keybindingProfiles.ContainsKey(codeplugIdentifier))
            {
                _keybindingProfiles[codeplugIdentifier] = new ChannelKeybindingProfile();
            }

            return _keybindingProfiles[codeplugIdentifier];
        }

        /// <summary>
        /// Sets a keybinding for a specific channel in a codeplug
        /// </summary>
        public void SetChannelKeybinding(string codeplugIdentifier, string channelName, string keybinding)
        {
            var profile = GetOrCreateProfile(codeplugIdentifier);
            
            if (string.IsNullOrWhiteSpace(keybinding))
            {
                profile.ChannelKeybindings.Remove(channelName);
            }
            else
            {
                profile.ChannelKeybindings[channelName] = keybinding;
            }

            SaveKeybindings();
            Console.WriteLine($"Set keybinding for {channelName}: {keybinding}");
        }

        /// <summary>
        /// Gets the keybinding for a specific channel, returns null if not set
        /// </summary>
        public string GetChannelKeybinding(string codeplugIdentifier, string channelName)
        {
            if (!_keybindingProfiles.TryGetValue(codeplugIdentifier, out var profile))
                return null;

            if (profile.ChannelKeybindings.TryGetValue(channelName, out var keybinding))
                return keybinding;

            return null;
        }

        /// <summary>
        /// Sets a toggle keybinding for a specific channel in a codeplug
        /// </summary>
        public void SetChannelToggleKeybinding(string codeplugIdentifier, string channelName, string keybinding)
        {
            var profile = GetOrCreateProfile(codeplugIdentifier);
            
            if (string.IsNullOrWhiteSpace(keybinding))
            {
                profile.ChannelToggleKeybindings.Remove(channelName);
            }
            else
            {
                profile.ChannelToggleKeybindings[channelName] = keybinding;
            }

            SaveKeybindings();
            Console.WriteLine($"Set toggle keybinding for {channelName}: {keybinding}");
        }

        /// <summary>
        /// Gets the toggle keybinding for a specific channel, returns null if not set
        /// </summary>
        public string GetChannelToggleKeybinding(string codeplugIdentifier, string channelName)
        {
            if (!_keybindingProfiles.TryGetValue(codeplugIdentifier, out var profile))
                return null;

            if (profile.ChannelToggleKeybindings.TryGetValue(channelName, out var keybinding))
                return keybinding;

            return null;
        }

        /// <summary>
        /// Gets all channels with keybindings for a codeplug
        /// </summary>
        public IEnumerable<string> GetChannelsWithKeybindings(string codeplugIdentifier)
        {
            if (_keybindingProfiles.TryGetValue(codeplugIdentifier, out var profile))
                return profile.ChannelKeybindings.Keys;

            return new List<string>();
        }

        /// <summary>
        /// Gets all channels with toggle keybindings for a codeplug
        /// </summary>
        public IEnumerable<string> GetChannelsWithToggleKeybindings(string codeplugIdentifier)
        {
            if (_keybindingProfiles.TryGetValue(codeplugIdentifier, out var profile))
                return profile.ChannelToggleKeybindings.Keys;

            return new List<string>();
        }

        /// <summary>
        /// Removes a keybinding for a specific channel
        /// </summary>
        public void RemoveChannelKeybinding(string codeplugIdentifier, string channelName)
        {
            var profile = GetOrCreateProfile(codeplugIdentifier);
            if (profile.ChannelKeybindings.Remove(channelName))
            {
                SaveKeybindings();
                Console.WriteLine($"Removed keybinding for {channelName}");
            }
        }

        /// <summary>
        /// Removes a toggle keybinding for a specific channel
        /// </summary>
        public void RemoveChannelToggleKeybinding(string codeplugIdentifier, string channelName)
        {
            var profile = GetOrCreateProfile(codeplugIdentifier);
            if (profile.ChannelToggleKeybindings.Remove(channelName))
            {
                SaveKeybindings();
                Console.WriteLine($"Removed toggle keybinding for {channelName}");
            }
        }

        /// <summary>
        /// Clears all keybindings for a codeplug profile
        /// </summary>
        public void ClearProfile(string codeplugIdentifier)
        {
            if (_keybindingProfiles.Remove(codeplugIdentifier))
            {
                SaveKeybindings();
                Console.WriteLine($"Cleared all keybindings for profile: {codeplugIdentifier}");
            }
        }

        /// <summary>
        /// Gets all keybinding profiles
        /// </summary>
        public Dictionary<string, ChannelKeybindingProfile> GetAllProfiles()
        {
            return new Dictionary<string, ChannelKeybindingProfile>(_keybindingProfiles);
        }

        /// <summary>
        /// Generates a unique identifier for a codeplug (using deterministic file path hash)
        /// </summary>
        public static string GenerateCodeplugIdentifier(string codeplugFilePath)
        {
            if (string.IsNullOrWhiteSpace(codeplugFilePath))
                return "default";

            // Use a combination of filename and deterministic path hash
            var fileName = Path.GetFileNameWithoutExtension(codeplugFilePath);
            
            // Use SHA256 for deterministic hashing (same path always produces same hash)
            using (var sha256 = SHA256.Create())
            {
                var pathBytes = Encoding.UTF8.GetBytes(codeplugFilePath.ToLowerInvariant());
                var hashBytes = sha256.ComputeHash(pathBytes);
                
                // Take first 4 bytes and convert to hex (8 characters)
                var pathHash = BitConverter.ToString(hashBytes, 0, 4).Replace("-", "");
                
                return $"{fileName}_{pathHash}";
            }
        }
    }
}
