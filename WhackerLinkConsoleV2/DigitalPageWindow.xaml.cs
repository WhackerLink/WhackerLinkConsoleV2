﻿/*
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
* Copyright (C) 2024 Caleb, K4PHP
* 
*/

using System.Windows;
using WhackerLinkLib.Models.Radio;

namespace WhackerLinkConsoleV2
{
    /// <summary>
    /// Interaction logic for DigitalPageWindow.xaml
    /// </summary>
    public partial class DigitalPageWindow : Window
    {
        public List<Codeplug.System> systems = new List<Codeplug.System>();

        public string DstId = string.Empty;
        public Codeplug.System RadioSystem = null;

        public DigitalPageWindow(List<Codeplug.System> systems)
        {
            InitializeComponent();
            this.systems = systems;

            SystemCombo.DisplayMemberPath = "Name";
            SystemCombo.ItemsSource = systems;
            SystemCombo.SelectedIndex = 0;
        }

        private void SendPageButton_Click(object sender, RoutedEventArgs e)
        {
            RadioSystem = SystemCombo.SelectedItem as Codeplug.System;
            DstId = DstIdText.Text;
            DialogResult = true;
            Close();
        }
    }
}
