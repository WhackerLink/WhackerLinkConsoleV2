using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WhackerLinkConsoleV2
{
    public class SettingsManager
    {
        private const string SettingsFilePath = "UserSettings.json";

        public bool ShowSystemStatus { get; set; } = true;
        public bool ShowChannels { get; set; } = true;
        public string LastCodeplugPath { get; set; } = null;

        public Dictionary<string, ChannelPosition> ChannelPositions { get; set; } = new Dictionary<string, ChannelPosition>();

        public void LoadSettings()
        {
            if (!File.Exists(SettingsFilePath)) return;

            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                var loadedSettings = JsonConvert.DeserializeObject<SettingsManager>(json);

                if (loadedSettings != null)
                {
                    ShowSystemStatus = loadedSettings.ShowSystemStatus;
                    ShowChannels = loadedSettings.ShowChannels;
                    LastCodeplugPath = loadedSettings.LastCodeplugPath;
                    ChannelPositions = loadedSettings.ChannelPositions ?? new Dictionary<string, ChannelPosition>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void UpdateChannelPosition(string channelName, double x, double y)
        {
            if (ChannelPositions.ContainsKey(channelName))
            {
                ChannelPositions[channelName].X = x;
                ChannelPositions[channelName].Y = y;
            }
            else
            {
                ChannelPositions[channelName] = new ChannelPosition { X = x, Y = y };
            }
        }
    }
}
