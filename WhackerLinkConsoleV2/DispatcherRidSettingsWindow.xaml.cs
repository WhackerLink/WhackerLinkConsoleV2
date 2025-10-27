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

namespace WhackerLinkConsoleV2
{
    public partial class DispatcherRidSettingsWindow : Window
    {
        private readonly SettingsManager _settingsManager;

        public DispatcherRidSettingsWindow(SettingsManager settingsManager)
        {
            InitializeComponent();
            _settingsManager = settingsManager;

            LoadSettings();
        }

        private void LoadSettings()
        {
            EnableOverrideCheckBox.IsChecked = _settingsManager.UseDispatcherRidOverride;
            RidTextBox.Text = _settingsManager.DispatcherRid;
        }

        private void EnableOverrideCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // The IsEnabled binding will handle enabling/disabling the textbox
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool enableOverride = EnableOverrideCheckBox.IsChecked == true;
                string rid = RidTextBox.Text.Trim();

                // Validate RID if override is enabled
                if (enableOverride)
                {
                    if (string.IsNullOrWhiteSpace(rid))
                    {
                        MessageBox.Show("Please enter a dispatcher RID.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Validate that RID is numeric
                    if (!uint.TryParse(rid, out uint ridValue))
                    {
                        MessageBox.Show("RID must be a valid numeric value.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Validate range (typical P25 RID range)
                    if (ridValue == 0 || ridValue > 16777215) // Max 24-bit value
                    {
                        MessageBox.Show("RID must be between 1 and 16777215.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Save settings
                _settingsManager.UseDispatcherRidOverride = enableOverride;
                _settingsManager.DispatcherRid = rid;
                _settingsManager.SaveSettings();

                MessageBox.Show(
                    enableOverride
                        ? $"Dispatcher RID {rid} will now be used for all transmissions."
                        : "RID override disabled. Codeplug RID will be used.",
                    "Settings Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving RID settings: {ex.Message}", "Error",
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
