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
using System.Windows.Input;

namespace WhackerLinkConsoleV2
{
    /// <summary>
    /// Helper class to parse and convert keybinding strings.
    /// Supports both single-key (e.g., "T", "F1") and multi-key (e.g., "Ctrl+T", "Ctrl+Alt+P")
    /// </summary>
    public static class KeybindingParser
    {
        /// <summary>
        /// Parses a keybinding string like "Ctrl+Alt+P" into ModifierKeys and Key
        /// </summary>
        /// <returns>Returns true if parsing was successful</returns>
        public static bool TryParseKeybinding(string keybindingString, out ModifierKeys modifiers, out Key key)
        {
            modifiers = ModifierKeys.None;
            key = Key.None;

            if (string.IsNullOrWhiteSpace(keybindingString))
            {
                Console.WriteLine("ERROR: Keybinding string is null or empty");
                return false;
            }

            try
            {
                var parts = keybindingString.Split('+');

                if (parts.Length == 0)
                {
                    Console.WriteLine($"ERROR: Invalid keybinding format: '{keybindingString}'");
                    return false;
                }

                // Handle single-key keybindings (e.g., "T", "P", "F1")
                if (parts.Length == 1)
                {
                    string keyString = parts[0].Trim();
                    if (!Enum.TryParse<Key>(keyString, true, out key))
                    {
                        Console.WriteLine($"ERROR: Unknown key: '{keyString}'");
                        return false;
                    }

                    if (key == Key.None)
                    {
                        Console.WriteLine($"ERROR: Key cannot be None");
                        return false;
                    }

                    return true;
                }

                // Handle multi-key keybindings (e.g., "Ctrl+T", "Ctrl+Alt+P")
                // Parse modifiers from all but the last part
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string part = parts[i].Trim().ToLower();

                    switch (part)
                    {
                        case "ctrl":
                        case "control":
                            modifiers |= ModifierKeys.Control;
                            break;
                        case "alt":
                            modifiers |= ModifierKeys.Alt;
                            break;
                        case "shift":
                            modifiers |= ModifierKeys.Shift;
                            break;
                        case "win":
                        case "windows":
                            modifiers |= ModifierKeys.Windows;
                            break;
                        default:
                            Console.WriteLine($"ERROR: Unknown modifier key: '{part}'");
                            return false;
                    }
                }

                // Parse the main key from the last part
                string keyStr = parts[parts.Length - 1].Trim();
                if (!Enum.TryParse<Key>(keyStr, true, out key))
                {
                    Console.WriteLine($"ERROR: Unknown key: '{keyStr}'");
                    return false;
                }

                if (key == Key.None)
                {
                    Console.WriteLine($"ERROR: Key cannot be None");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception parsing keybinding '{keybindingString}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts ModifierKeys and Key back to a keybinding string
        /// </summary>
        public static string KeybindingToString(ModifierKeys modifiers, Key key)
        {
            var result = ModifiersToString(modifiers);
            if (!string.IsNullOrEmpty(result))
                result += "+";
            result += key.ToString();

            return result;
        }

        /// <summary>
        /// Converts ModifierKeys to a string representation (without the final key)
        /// </summary>
        public static string ModifiersToString(ModifierKeys modifiers)
        {
            var result = "";

            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                result += "Ctrl+";
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                result += "Alt+";
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                result += "Shift+";
            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                result += "Win+";

            return result.TrimEnd('+');
        }
    }
}
