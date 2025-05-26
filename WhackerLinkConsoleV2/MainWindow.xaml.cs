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
* Copyright (C) 2024-2025 Caleb, K4PHP
* Copyright (C) 2025 J. Dean
* 
*/

using Microsoft.Win32;
using System;
using System.Timers;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WhackerLinkLib.Models.Radio;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using WhackerLinkConsoleV2.Controls;
using WebSocketManager = WhackerLinkLib.Managers.WebSocketManager;
using System.Windows.Media;
using WhackerLinkLib.Utils;
using WhackerLinkLib.Models;
using System.Net;
using NAudio.Wave;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models.IOSP;
using fnecore.P25;
using fnecore;
using Microsoft.VisualBasic;
using System.Text;
using Constants = fnecore.Constants;
using System.Security.Cryptography;
using fnecore.P25.LC.TSBK;
using WebSocketSharp;
using NWaves.Signals;
using static WhackerLinkConsoleV2.P25Crypto;
using static WhackerLinkLib.Models.Radio.Codeplug;
using System.Threading.Channels;

namespace WhackerLinkConsoleV2
{
    public partial class MainWindow : Window
    {
        public Codeplug Codeplug { get; set; }
        private bool isEditMode = false;

        private bool globalPttState = false;

        private UIElement _draggedElement;
        private Point _startPoint;
        private double _offsetX;
        private double _offsetY;
        private bool _isDragging;

        private SettingsManager _settingsManager = new SettingsManager();
        private SelectedChannelsManager _selectedChannelsManager;

        private List<(double ToneA, double ToneB)> _selectedToneSets = new List<(double, double)>();

        private FlashingBackgroundManager _flashingManager;
        private WaveFilePlaybackManager _emergencyAlertPlayback;
        private WebSocketManager _webSocketManager = new WebSocketManager();

        private ChannelBox playbackChannelBox;

        CallHistoryWindow callHistoryWindow = new CallHistoryWindow();

        public static string PLAYBACKTG = "LOCPLAYBACK";
        public static string PLAYBACKSYS = "LOCPLAYBACKSYS";
        public static string PLAYBACKCHNAME = "PLAYBACK";

        private readonly WaveInEvent _waveIn;
        private readonly AudioManager _audioManager;

        private static System.Timers.Timer _channelHoldTimer;

        private Dictionary<string, SlotStatus> systemStatuses = new Dictionary<string, SlotStatus>();
        private FneSystemManager _fneSystemManager = new FneSystemManager();

        private bool cryptodev = true;

        private static HashSet<uint> usedRids = new HashSet<uint>();

        List<Tuple<uint, uint>> fneAffs = new List<Tuple<uint, uint>>();

        public MainWindow()
        {
#if !DEBUG
            ConsoleNative.ShowConsole();
#endif
            InitializeComponent();
            _settingsManager.LoadSettings();
            _selectedChannelsManager = new SelectedChannelsManager();
            _flashingManager = new FlashingBackgroundManager(null, ChannelsCanvas, null, this);
            _emergencyAlertPlayback = new WaveFilePlaybackManager(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "emergency.wav"));

            _channelHoldTimer = new System.Timers.Timer(10000);
            _channelHoldTimer.Elapsed += OnHoldTimerElapsed;
            _channelHoldTimer.AutoReset = true;
            _channelHoldTimer.Enabled = true;

            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(8000, 16, 1)
            };
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.RecordingStopped += WaveIn_RecordingStopped;

            _waveIn.StartRecording();

            _audioManager = new AudioManager(_settingsManager);

            _selectedChannelsManager.SelectedChannelsChanged += SelectedChannelsChanged;
            Loaded += MainWindow_Loaded;
        }

        private class YamlConfig
        {
            public List<Codeplug.Tone> Tones { get; set; }
        }

        private void OpenCodeplug_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Codeplug Files (*.yml)|*.yml|All Files (*.*)|*.*",
                Title = "Open Codeplug"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                LoadCodeplug(openFileDialog.FileName);

                _settingsManager.LastCodeplugPath = openFileDialog.FileName;
                _settingsManager.SaveSettings();
            }
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("UserSettings.json"))
                File.Delete("UserSettings.json");
        }

        private void LoadCodeplug(string filePath)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var yaml = File.ReadAllText(filePath);
                Codeplug = deserializer.Deserialize<Codeplug>(yaml);

                GenerateChannelWidgets();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading codeplug: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateChannelWidgets()
        {
            ChannelsCanvas.Children.Clear();
            double offsetX = 20;
            double offsetY = 20;

            if (Codeplug != null)
            {
                foreach (var system in Codeplug.Systems)
                {
                    var systemStatusBox = new SystemStatusBox(system.Name, system.Address, system.Port);

                    if (_settingsManager.SystemStatusPositions.TryGetValue(system.Name, out var position))
                    {
                        Canvas.SetLeft(systemStatusBox, position.X);
                        Canvas.SetTop(systemStatusBox, position.Y);
                    }
                    else
                    {
                        Canvas.SetLeft(systemStatusBox, offsetX);
                        Canvas.SetTop(systemStatusBox, offsetY);
                    }

                    systemStatusBox.MouseLeftButtonDown += SystemStatusBox_MouseLeftButtonDown;
                    systemStatusBox.MouseMove += SystemStatusBox_MouseMove;
                    systemStatusBox.MouseRightButtonDown += SystemStatusBox_MouseRightButtonDown;

                    ChannelsCanvas.Children.Add(systemStatusBox);

                    offsetX += 225;
                    if (offsetX + 220 > ChannelsCanvas.ActualWidth)
                    {
                        offsetX = 20;
                        offsetY += 106;
                    }

                    if (File.Exists(system.AliasPath))
                        system.RidAlias = AliasTools.LoadAliases(system.AliasPath);

                    if (!system.IsDvm)
                    {
                        _webSocketManager.AddWebSocketHandler(system.Name);

                        IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);
                        handler.OnVoiceChannelResponse += HandleVoiceResponse;
                        handler.OnVoiceChannelRelease += HandleVoiceRelease;
                        handler.OnEmergencyAlarmResponse += HandleEmergencyAlarmResponse;
                        handler.OnAudioData += HandleReceivedAudio;
                        handler.OnAffiliationUpdate += HandleAffiliationUpdate;
                        handler.OnCallAlert += HandleCallAlert;

                        handler.OnUnitRegistrationResponse += (response) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (response.Status == (int)ResponseType.GRANT)
                                {
                                    systemStatusBox.Background = (Brush)new BrushConverter().ConvertFrom("#FF00BC48");
                                    systemStatusBox.ConnectionState = "Connected";

                                    foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
                                    {
                                        if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                                            continue;

                                        Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                                        Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                                        if (!system.IsDvm)
                                        {
                                            IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                                            if (channel.IsSelected && handler.IsConnected)
                                            {
                                                Console.WriteLine("sending WLINK master aff");

                                                Task.Run(() =>
                                                {
                                                    GRP_AFF_REQ release = new GRP_AFF_REQ
                                                    {
                                                        SrcId = system.Rid,
                                                        DstId = cpgChannel.Tgid,
                                                        Site = system.Site
                                                    };

                                                    handler.SendMessage(release.GetData());
                                                });
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    systemStatusBox.Background = new SolidColorBrush(Colors.Red);
                                    systemStatusBox.ConnectionState = "Disconnected";
                                }
                            });
                        };

                        handler.OnClose += () =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                systemStatusBox.Background = new SolidColorBrush(Colors.Red);
                                systemStatusBox.ConnectionState = "Disconnected";
                            });
                        };

                        handler.OnOpen += () =>
                        {
                            Console.WriteLine("Peer connected");
                            U_REG_REQ release = new U_REG_REQ
                            {
                                SrcId = system.Rid,
                                Site = system.Site
                            };

                            handler.SendMessage(release.GetData());
                        };

                        handler.OnReconnecting += () =>
                        {
                            Console.WriteLine("Peer reconnecting");
                        };

                        Task.Run(() =>
                        {
                            handler.Connect(system.Address, system.Port, system.AuthKey);

                            handler.OnGroupAffiliationResponse += (response) => { /* TODO */ };

                            if (!handler.IsConnected)
                            {
                                systemStatusBox.Background = new SolidColorBrush(Colors.Red);
                                systemStatusBox.ConnectionState = "Disconnected";
                            }
                        });
                    } else {
                        _fneSystemManager.AddFneSystem(system.Name, system, this);

                        PeerSystem peer = _fneSystemManager.GetFneSystem(system.Name);

                        peer.peer.PeerConnected += (sender, response) =>
                        {
                            Console.WriteLine("FNE Peer connected");

                            Dispatcher.Invoke(() =>
                            {
                                systemStatusBox.Background = (Brush)new BrushConverter().ConvertFrom("#FF00BC48");
                                systemStatusBox.ConnectionState = "Connected";
                            });
                        };


                        peer.peer.PeerDisconnected += (response) =>
                        {
                            Console.WriteLine("FNE Peer disconnected");

                            Dispatcher.Invoke(() =>
                            {
                                systemStatusBox.Background = new SolidColorBrush(Colors.Red);
                                systemStatusBox.ConnectionState = "Disconnected";
                            });
                        };

                        Task.Run(() =>
                        {
                            peer.Start();
                        });
                    }

                    if (!_settingsManager.ShowSystemStatus)
                        systemStatusBox.Visibility = Visibility.Collapsed;

                }
            }


            if (_settingsManager.ShowChannels && Codeplug != null)
            {
                foreach (var zone in Codeplug.Zones)
                {
                    foreach (var channel in zone.Channels)
                    {
                        var channelBox = new ChannelBox(_selectedChannelsManager, _audioManager, channel.Name, channel.System, channel.Tgid);

                        //channelBox.crypter.AddKey(channel.GetKeyId(), channel.GetAlgoId(), channel.GetEncryptionKey());

                        systemStatuses.Add(channel.Name, new SlotStatus());

                        if (_settingsManager.ChannelPositions.TryGetValue(channel.Name, out var position))
                        {
                            Canvas.SetLeft(channelBox, position.X);
                            Canvas.SetTop(channelBox, position.Y);
                        }
                        else
                        {
                            Canvas.SetLeft(channelBox, offsetX);
                            Canvas.SetTop(channelBox, offsetY);
                        }

                        channelBox.PTTButtonClicked += ChannelBox_PTTButtonClicked;
                        channelBox.PageButtonClicked += ChannelBox_PageButtonClicked;
                        channelBox.HoldChannelButtonClicked += ChannelBox_HoldChannelButtonClicked;

                        channelBox.MouseLeftButtonDown += ChannelBox_MouseLeftButtonDown;
                        channelBox.MouseMove += ChannelBox_MouseMove;
                        channelBox.MouseRightButtonDown += ChannelBox_MouseRightButtonDown;
                        ChannelsCanvas.Children.Add(channelBox);

                        offsetX += 225;

                        if (offsetX + 220 > ChannelsCanvas.ActualWidth)
                        {
                            offsetX = 20;
                            offsetY += 106;
                        }
                    }
                }
            }

            if (_settingsManager.ShowAlertTones && Codeplug != null)
            {
                foreach (var alertPath in _settingsManager.AlertToneFilePaths)
                {
                    var alertTone = new AlertTone(alertPath)
                    {
                        IsEditMode = isEditMode
                    };

                    alertTone.OnAlertTone += SendAlertTone;

                    if (_settingsManager.AlertTonePositions.TryGetValue(alertPath, out var position))
                    {
                        Canvas.SetLeft(alertTone, position.X);
                        Canvas.SetTop(alertTone, position.Y);
                    }
                    else
                    {
                        Canvas.SetLeft(alertTone, 20);
                        Canvas.SetTop(alertTone, 20);
                    }

                    alertTone.MouseRightButtonUp += AlertTone_MouseRightButtonUp;

                    ChannelsCanvas.Children.Add(alertTone);
                }
            }

            // Add ToneSet boxes if enabled
            if (_settingsManager.ShowQCTones && Codeplug != null && Codeplug?.Tones != null)
            {
                foreach (var tone in Codeplug.Tones)
                {
                    var toneSetControl = new ToneSet(tone.Name, tone.ToneA, tone.ToneB);

                    // Hook up events
                    toneSetControl.PlayClicked += async (s, e) =>
                    {
                        if (_selectedToneSets.Count > 0)
                        {
                            var selectedTones = _selectedToneSets.ToList();
                            var initiallyActiveChannels = _selectedChannelsManager
                                .GetSelectedChannels()
                                .Where(c => c.PageState)
                                .ToList();

                            for (int i = 0; i < selectedTones.Count; i++)
                            {
                                var selected = selectedTones[i];
                                var key = (selected.ToneA, selected.ToneB);

                                // Check if any of the original channels have lost their PageState
                                var prematurelyCleared = initiallyActiveChannels
                                    .Where(c => !c.PageState)
                                    .ToList();

                                if (prematurelyCleared.Any())
                                {
                                    // Pause logic: restore PageState for affected channels
                                    foreach (var ch in prematurelyCleared)
                                    {
                                        ch.PageState = true;
                                        Dispatcher.Invoke(() =>
                                        {
                                            ch.PageSelectButton.Background = ch.orangeGradient;
                                        });
                                    }

                                    // Wait a moment to visually reflect the reset before continuing
                                    await Task.Delay(300);
                                }

                                // Play the tone
                                await PlayTone(selected.ToneA.ToString(), selected.ToneB.ToString());

                                // Remove from selected set
                                _selectedToneSets.Remove(key);

                                // Deselect the tone visually
                                foreach (var child in ChannelsCanvas.Children)
                                {
                                    if (child is ToneSet ts && ts.ToneA == key.ToneA && ts.ToneB == key.ToneB)
                                    {
                                        ts.SetSelected(false);
                                        break;
                                    }
                                }
                            }

                            // Now that all tones are done, clean up page state
                            foreach (var ch in initiallyActiveChannels)
                            {
                                ch.PageState = false;
                                ch.PageSelectButton.Background = ch.grayGradient;
                            }
                        }
                        else
                        {
                            var hasActivePage = _selectedChannelsManager.GetSelectedChannels().Any(c => c.PageState);
                            if (hasActivePage)
                            {
                                await PlayTone(tone.ToneA.ToString(), tone.ToneB.ToString());

                                // Clear PageState after this tone
                                foreach (var channel in _selectedChannelsManager.GetSelectedChannels())
                                {
                                    channel.PageState = false;
                                    channel.PageSelectButton.Background = channel.grayGradient;
                                }
                            }
                        }
                    };


                    toneSetControl.SelectToggled += (s, e) =>
                    {
                        var key = (tone.ToneA, tone.ToneB);
                        if (_selectedToneSets.Contains(key))
                        {
                            _selectedToneSets.Remove(key);
                            toneSetControl.SetSelected(false);
                        }
                        else
                        {
                            _selectedToneSets.Add(key);
                            toneSetControl.SetSelected(true);
                        }
                    };

                    // Position like ToneSet Boxes using same layout logic
                    if (_settingsManager.QCToneSetPositions.TryGetValue(tone.Name, out var position))
                    {
                        Canvas.SetLeft(toneSetControl, position.X);
                        Canvas.SetTop(toneSetControl, position.Y);
                    }
                    else
                    {
                        Canvas.SetLeft(toneSetControl, offsetX);
                        Canvas.SetTop(toneSetControl, offsetY);
                    }

                    toneSetControl.MouseLeftButtonDown += ToneSet_MouseLeftButtonDown;
                    toneSetControl.MouseMove += ToneSet_MouseMove;
                    toneSetControl.MouseRightButtonDown += ToneSet_MouseRightButtonDown;

                    ChannelsCanvas.Children.Add(toneSetControl);

                    offsetX += 225;

                    if (offsetX + 220 > ChannelsCanvas.ActualWidth)
                    {
                        offsetX = 20;
                        offsetY += 106;
                    }
                }
            }



            playbackChannelBox = new ChannelBox(_selectedChannelsManager, _audioManager, PLAYBACKCHNAME, PLAYBACKSYS, PLAYBACKTG);

            if (_settingsManager.ChannelPositions.TryGetValue(PLAYBACKCHNAME, out var pos))
            {
                Canvas.SetLeft(playbackChannelBox, pos.X);
                Canvas.SetTop(playbackChannelBox, pos.Y);
            }
            else
            {
                Canvas.SetLeft(playbackChannelBox, offsetX);
                Canvas.SetTop(playbackChannelBox, offsetY);
            }

            playbackChannelBox.PTTButtonClicked += ChannelBox_PTTButtonClicked;
            playbackChannelBox.PageButtonClicked += ChannelBox_PageButtonClicked;
            playbackChannelBox.HoldChannelButtonClicked += ChannelBox_HoldChannelButtonClicked;

            playbackChannelBox.MouseLeftButtonDown += ChannelBox_MouseLeftButtonDown;
            playbackChannelBox.MouseMove += ChannelBox_MouseMove;
            playbackChannelBox.MouseRightButtonDown += ChannelBox_MouseRightButtonDown;
            ChannelsCanvas.Children.Add(playbackChannelBox);

            //offsetX += 225;

            //if (offsetX + 220 > ChannelsCanvas.ActualWidth)
            //{
            //    offsetX = 20;
            //    offsetY += 106;
            //}

            AdjustCanvasHeight();
        }

        private const int GridSize = 5;

        private void ToneSet_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode || !(sender is UIElement element)) return;

            _draggedElement = element;
            _startPoint = e.GetPosition(ChannelsCanvas);
            _offsetX = _startPoint.X - Canvas.GetLeft(_draggedElement);
            _offsetY = _startPoint.Y - Canvas.GetTop(_draggedElement);
            _isDragging = true;

            element.CaptureMouse();
        }

        private void ToneSet_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isEditMode || !_isDragging || _draggedElement == null) return;

            Point currentPosition = e.GetPosition(ChannelsCanvas);

            // Calculate the new position with snapping to the grid
            double newLeft = Math.Round((currentPosition.X - _offsetX) / GridSize) * GridSize;
            double newTop = Math.Round((currentPosition.Y - _offsetY) / GridSize) * GridSize;

            // Ensure the box stays within canvas bounds
            newLeft = Math.Max(0, Math.Min(newLeft, ChannelsCanvas.ActualWidth - _draggedElement.RenderSize.Width));
            newTop = Math.Max(0, Math.Min(newTop, ChannelsCanvas.ActualHeight - _draggedElement.RenderSize.Height));

            // Apply snapped position
            Canvas.SetLeft(_draggedElement, newLeft);
            Canvas.SetTop(_draggedElement, newTop);

            // Save the new position if it's a ToneSet
            if (_draggedElement is ToneSet toneSet)
            {
                _settingsManager.UpdateQCToneSetPosition(toneSet.ToneName, newLeft, newTop);
            }

            AdjustCanvasHeight();
        }

        private void ToneSet_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode || !_isDragging || _draggedElement == null) return;

            _isDragging = false;
            _draggedElement.ReleaseMouseCapture();
            _draggedElement = null;
        }


        private async void ToneSet_PlayClicked(object sender, EventArgs e)
        {
            if (sender is ToneSet toneSet)
            {
                if (_selectedToneSets.Count > 0)
                {
                    foreach (var selected in _selectedToneSets)
                    {
                        await PlayTone(selected.ToneA.ToString(), selected.ToneB.ToString());
                    }
                }
                else
                {
                    await PlayTone(toneSet.ToneA.ToString(), toneSet.ToneB.ToString());
                }
            }
        }

        private void ToneSet_SelectClicked(object sender, EventArgs e)
        {
            if (sender is ToneSet toneSet)
            {
                var key = (toneSet.ToneA, toneSet.ToneB);

                if (_selectedToneSets.Contains(key))
                {
                    _selectedToneSets.Remove(key);
                    toneSet.SetSelected(false);
                }
                else
                {
                    _selectedToneSets.Add(key);
                    toneSet.SetSelected(true);
                }
            }
        }

        private void WaveIn_RecordingStopped(object sender, EventArgs e)
        {
            /* stub */
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            bool isAnyTgOn = false;

            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                {
                    playbackChannelBox.IsReceiving = true;
                    continue;
                }

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                Task.Run(() =>
                {

                    if (!system.IsDvm)
                    {
                        IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                        if (channel.IsSelected && channel.VoiceChannel != null && channel.PttState)
                        {
                            isAnyTgOn = true;

                            object voicePaket = new
                            {
                                type = PacketType.AUDIO_DATA,
                                data = new
                                {
                                    Data = e.Buffer,
                                    VoiceChannel = new VoiceChannel
                                    {
                                        Frequency = channel.VoiceChannel,
                                        DstId = cpgChannel.Tgid,
                                        SrcId = system.Rid,
                                        Site = system.Site
                                    },
                                    Site = system.Site
                                }
                            };

#if DEBUG
                            Console.WriteLine($"WLINK AUDIO_DATA SrcId: {system.Rid}; DstId: {cpgChannel.Tgid}; Freq: {channel.VoiceChannel}");
#endif

                            handler.SendMessage(voicePaket);
                        }
                    }
                    else
                    {
                        PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                        if (channel.IsSelected && channel.PttState)
                        {
                            isAnyTgOn = true;

                            int samples = 320;

                            channel.chunkedPcm = AudioConverter.SplitToChunks(e.Buffer);

                            foreach (byte[] chunk in channel.chunkedPcm)
                            {
                                if (chunk.Length == samples)
                                {
                                    P25EncodeAudioFrame(chunk, handler, channel, cpgChannel, system);
                                }
                                else
                                {
                                    Console.WriteLine("bad sample length: " + chunk.Length);
                                }
                            }
                        }
                    }
                });
            }

            if (isAnyTgOn && playbackChannelBox.IsSelected)
                _audioManager.AddTalkgroupStream(PLAYBACKTG, e.Buffer);
        }

        private void SelectedChannelsChanged()
        {
            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);
         
                if (!system.IsDvm) {
                    IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                    if (channel.IsSelected && handler.IsConnected)
                    {
                        Console.WriteLine("sending WLINK master aff");

                        Task.Run(() =>
                        {
                            GRP_AFF_REQ release = new GRP_AFF_REQ
                            {
                                SrcId = system.Rid,
                                DstId = cpgChannel.Tgid,
                                Site = system.Site
                            };

                            handler.SendMessage(release.GetData());
                        });
                    }
                } else
                {
                    PeerSystem fne = _fneSystemManager.GetFneSystem(system.Name);

                    if (channel.IsSelected)
                    {
                        uint newTgid = UInt32.Parse(cpgChannel.Tgid);
                        bool exists = fneAffs.Any(aff => aff.Item2 == newTgid);

                        if (cpgChannel.GetAlgoId() != 0 && cpgChannel.GetKeyId() != 0)
                            fne.peer.SendMasterKeyRequest(cpgChannel.GetAlgoId(), cpgChannel.GetKeyId());

                        if (!exists)
                            fneAffs.Add(new Tuple<uint, uint>(GetUniqueRid(system.Rid), newTgid));

                        //Console.WriteLine("FNE Affiliations:");
                        //foreach (var aff in fneAffs)
                        //{
                        //    Console.WriteLine($"  RID: {aff.Item1}, TGID: {aff.Item2}");
                        //}
                    }
                }
            }

            foreach (Codeplug.System system in Codeplug.Systems)
            {
                if (system.IsDvm)
                {
                    PeerSystem fne = _fneSystemManager.GetFneSystem(system.Name);
                    //fne.peer.SendMasterAffiliationUpdate(fneAffs);
                }
            }
        }

        private void AudioSettings_Click(object sender, RoutedEventArgs e)
        {
            List<Codeplug.Channel> channels = Codeplug?.Zones.SelectMany(z => z.Channels).ToList() ?? new List<Codeplug.Channel>();

            AudioSettingsWindow audioSettingsWindow = new AudioSettingsWindow(_settingsManager, _audioManager, channels);
            audioSettingsWindow.ShowDialog();
        }

        private void P25Page_Click(object sender, RoutedEventArgs e)
        {
            DigitalPageWindow pageWindow = new DigitalPageWindow(Codeplug.Systems);
            pageWindow.Owner = this;
            if (pageWindow.ShowDialog() == true)
            {
                if (!pageWindow.RadioSystem.IsDvm)
                {
                    IPeer handler = _webSocketManager.GetWebSocketHandler(pageWindow.RadioSystem.Name);

                    CALL_ALRT_REQ callAlert = new CALL_ALRT_REQ
                    {
                        SrcId = pageWindow.RadioSystem.Rid,
                        DstId = pageWindow.DstId
                    };

                    handler.SendMessage(callAlert.GetData());
                }
                else
                {
                    PeerSystem handler = _fneSystemManager.GetFneSystem(pageWindow.RadioSystem.Name);
                    IOSP_CALL_ALRT callAlert = new IOSP_CALL_ALRT(UInt32.Parse(pageWindow.DstId), UInt32.Parse(pageWindow.RadioSystem.Rid));

                    RemoteCallData callData = new RemoteCallData
                    {
                        SrcId = UInt32.Parse(pageWindow.RadioSystem.Rid),
                        DstId = UInt32.Parse(pageWindow.DstId),
                        LCO = P25Defines.TSBK_IOSP_CALL_ALRT
                    };

                    byte[] tsbk = new byte[P25Defines.P25_TSBK_LENGTH_BYTES];

                    callAlert.Encode(ref tsbk, true, true);

                    handler.SendP25TSBK(callData, tsbk);

                    Console.WriteLine("sent page");
                }
            }
        }

        private async void ManualPage_Click(object sender, RoutedEventArgs e)
        {
            QuickCallPage pageWindow = new QuickCallPage();
            pageWindow.Owner = this;
            if (pageWindow.ShowDialog() == true)
            {
                foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
                {
                    Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                    Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                    if (channel.PageState)
                    {
                        ToneGenerator generator = new ToneGenerator();

                        double toneADuration = 1.0;
                        double toneBDuration = 3.0;

                        byte[] toneA = generator.GenerateTone(Double.Parse(pageWindow.ToneA), toneADuration);
                        byte[] toneB = generator.GenerateTone(Double.Parse(pageWindow.ToneB), toneBDuration);

                        byte[] combinedAudio = new byte[toneA.Length + toneB.Length];
                        Buffer.BlockCopy(toneA, 0, combinedAudio, 0, toneA.Length);
                        Buffer.BlockCopy(toneB, 0, combinedAudio, toneA.Length, toneB.Length);

                        int chunkSize = 1600;

                        if (system.IsDvm)
                            chunkSize = 320;

                        int totalChunks = (combinedAudio.Length + chunkSize - 1) / chunkSize;

                        Task.Run(() =>
                        {
                            //_waveProvider.ClearBuffer();
                            _audioManager.AddTalkgroupStream(cpgChannel.Tgid, combinedAudio);
                        });

                        await Task.Run(() =>
                        {
                            for (int i = 0; i < totalChunks; i++)
                            {
                                int offset = i * chunkSize;
                                int size = Math.Min(chunkSize, combinedAudio.Length - offset);

                                byte[] chunk = new byte[chunkSize];
                                Buffer.BlockCopy(combinedAudio, offset, chunk, 0, size);

                                if (!system.IsDvm)
                                {
                                    IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                                    AudioPacket voicePacket = new AudioPacket
                                    {
                                        Data = chunk,
                                        VoiceChannel = new VoiceChannel
                                        {
                                            Frequency = channel.VoiceChannel,
                                            DstId = cpgChannel.Tgid,
                                            SrcId = system.Rid,
                                            Site = system.Site
                                        },
                                        Site = system.Site,
                                        LopServerVocode = true
                                    };

                                    handler.SendMessage(voicePacket.GetData());
                                } else
                                {
                                    PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                                    if (chunk.Length == 320)
                                    {
                                        P25EncodeAudioFrame(chunk, handler, channel, cpgChannel, system);
                                    }
                                }
                            }
                        });

                        double totalDurationMs = (toneADuration + toneBDuration) * 1000 + 750;
                        await Task.Delay((int)totalDurationMs);

                        if (!system.IsDvm)
                        {
                            IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                            GRP_VCH_RLS release = new GRP_VCH_RLS
                            {
                                SrcId = system.Rid,
                                DstId = cpgChannel.Tgid,
                                Channel = channel.VoiceChannel,
                                Site = system.Site
                            };

                            handler.SendMessage(release.GetData());
                        } else
                        {
                            PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                            await Task.Delay(4000);

                            handler.SendP25TDU(UInt32.Parse(system.Rid), UInt32.Parse(cpgChannel.Tgid), false);
                        }

                        Dispatcher.Invoke(() =>
                        {
                            //channel.PageState = false; // TODO: Investigate
                            channel.PageSelectButton.Background = channel.grayGradient;
                        });
                    }
                }
            }
        }

        private void SendAlertTone(AlertTone e)
        {
            Task.Run(() => SendAlertTone(e.AlertFilePath));
        }

        private void SendAlertTone(string filePath, bool forHold = false)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
                    {
                        if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                            continue;

                        Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                        Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                        if (channel.PageState || (forHold && channel.HoldState))
                        {
                            byte[] pcmData;

                            Task.Run(async () => {
                                using (var waveReader = new WaveFileReader(filePath))
                                {
                                    if (waveReader.WaveFormat.Encoding != WaveFormatEncoding.Pcm ||
                                        waveReader.WaveFormat.SampleRate != 8000 ||
                                        waveReader.WaveFormat.BitsPerSample != 16 ||
                                        waveReader.WaveFormat.Channels != 1)
                                    {
                                        MessageBox.Show("The alert tone must be PCM 16-bit, Mono, 8000Hz format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }

                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        waveReader.CopyTo(ms);
                                        pcmData = ms.ToArray();
                                    }
                                }

                                int chunkSize = 1600;
                                int totalChunks = (pcmData.Length + chunkSize - 1) / chunkSize;

                                if (pcmData.Length % chunkSize != 0)
                                {
                                    byte[] paddedData = new byte[totalChunks * chunkSize];
                                    Buffer.BlockCopy(pcmData, 0, paddedData, 0, pcmData.Length);
                                    pcmData = paddedData;
                                }

                                Task.Run(() =>
                                {
                                    _audioManager.AddTalkgroupStream(cpgChannel.Tgid, pcmData);
                                });

                                DateTime startTime = DateTime.UtcNow;

                                for (int i = 0; i < totalChunks; i++)
                                {
                                    int offset = i * chunkSize;
                                    byte[] chunk = new byte[chunkSize];
                                    Buffer.BlockCopy(pcmData, offset, chunk, 0, chunkSize);

                                    if (!system.IsDvm)
                                    {
                                        IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                                        AudioPacket voicePacket = new AudioPacket
                                        {
                                            Data = chunk,
                                            VoiceChannel = new VoiceChannel
                                            {
                                                Frequency = channel.VoiceChannel,
                                                DstId = cpgChannel.Tgid,
                                                SrcId = system.Rid,
                                                Site = system.Site
                                            },
                                            Site = system.Site,
                                            LopServerVocode = true
                                        };

                                        handler.SendMessage(voicePacket.GetData());
                                    }
                                    else
                                    {
                                        PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                                        channel.chunkedPcm = AudioConverter.SplitToChunks(chunk);

                                        foreach (byte[] smallchunk in channel.chunkedPcm)
                                        {
                                            if (smallchunk.Length == 320)
                                            {
                                                P25EncodeAudioFrame(smallchunk, handler, channel, cpgChannel, system);
                                            }
                                        }
                                    }

                                    DateTime nextPacketTime = startTime.AddMilliseconds((i + 1) * 100);
                                    TimeSpan waitTime = nextPacketTime - DateTime.UtcNow;

                                    if (waitTime.TotalMilliseconds > 0)
                                    {
                                        await Task.Delay(waitTime);
                                    }
                                }

                                double totalDurationMs = ((double)pcmData.Length / 16000) + 250;
                                await Task.Delay((int)totalDurationMs);

                                if (!system.IsDvm)
                                {
                                    IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                                    GRP_VCH_RLS release = new GRP_VCH_RLS
                                    {
                                        SrcId = system.Rid,
                                        DstId = cpgChannel.Tgid,
                                        Channel = channel.VoiceChannel,
                                        Site = system.Site
                                    };

                                    Dispatcher.Invoke(() =>
                                    {
                                        handler.SendMessage(release.GetData());

                                        if (forHold)
                                            channel.PttButton.Background = channel.grayGradient;
                                        else
                                            channel.PageState = false;
                                    });
                                } else
                                {
                                    PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                                    await Task.Delay(3000);

                                    handler.SendP25TDU(UInt32.Parse(system.Rid), UInt32.Parse(cpgChannel.Tgid), false);

                                    Dispatcher.Invoke(() =>
                                    {
                                        if (forHold)
                                            channel.PttButton.Background = channel.grayGradient;
                                        else
                                            channel.PageState = false;
                                    });
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to process alert tone: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Alert file not set or file not found.", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SelectWidgets_Click(object sender, RoutedEventArgs e)
        {
            WidgetSelectionWindow widgetSelectionWindow = new WidgetSelectionWindow();
            widgetSelectionWindow.Owner = this;
            if (widgetSelectionWindow.ShowDialog() == true)
            {
                _settingsManager.ShowSystemStatus = widgetSelectionWindow.ShowSystemStatus;
                _settingsManager.ShowChannels = widgetSelectionWindow.ShowChannels;
                _settingsManager.ShowAlertTones = widgetSelectionWindow.ShowAlertTones;
                _settingsManager.ShowQCTones = widgetSelectionWindow.ShowQCTones;

                GenerateChannelWidgets();
                _settingsManager.SaveSettings();
            }
        }

        private void HandleEmergencyAlarmResponse(EMRG_ALRM_RSP response)
        {
            bool forUs = false;

            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (response.DstId == cpgChannel.Tgid)
                {
                    forUs = true;
                    channel.Emergency = true;
                    channel.LastSrcId = response.SrcId;
                }
            }

            if (forUs)
            {
                Dispatcher.Invoke(() =>
                {
                    _flashingManager.Start();
                    _emergencyAlertPlayback.Start();
                });
            }
        }

        private void HandleCallAlert(CALL_ALRT request)
        {
            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (system.IsDvm)
                    continue;

                IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                if (request.DstId == system.Rid)
                {

                    ACK_RSP ack = new ACK_RSP
                    {
                        SrcId = request.SrcId,
                        DstId = request.DstId,
                        Service = PacketType.CALL_ALRT
                    };

                    handler.SendMessage(ack.GetData());
                }
            }
        }

        private void HandleReceivedAudio(AudioPacket audioPacket)
        {
            bool shouldReceive = false;
            string talkgroupId = audioPacket.VoiceChannel.DstId;

            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (system.IsDvm)
                    continue;

                if (audioPacket.VoiceChannel.SrcId != system.Rid && audioPacket.VoiceChannel.Frequency == channel.VoiceChannel && audioPacket.VoiceChannel.DstId == cpgChannel.Tgid)
                    shouldReceive = true;
            }

            if (shouldReceive)
                _audioManager.AddTalkgroupStream(talkgroupId, audioPacket.Data);
        }

        private void HandleAffiliationUpdate(AFF_UPDATE affUpdate)
        {
            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (system.IsDvm)
                    continue;

                IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                bool ridExists = affUpdate.Affiliations.Any(aff => aff.SrcId == system.Rid);
                bool tgidExists = affUpdate.Affiliations.Any(aff => aff.DstId == cpgChannel.Tgid);

                if (ridExists && tgidExists)
                {
                    //Console.WriteLine("rid aff'ed");
                }
                else
                {
                    //Console.WriteLine("rid not aff'ed");
                    Task.Run(() =>
                    {
                        GRP_AFF_REQ affReq = new GRP_AFF_REQ
                        {
                            SrcId = system.Rid,
                            DstId = cpgChannel.Tgid,
                            Site = system.Site
                        };

                        handler.SendMessage(affReq.GetData());
                    });
                }
            }
        }

        private void HandleVoiceRelease(GRP_VCH_RLS response)
        {
            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (system.IsDvm)
                    continue;

                IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                if (response.DstId == cpgChannel.Tgid && response.SrcId != system.Rid)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (channel.IsSelected)
                        {
                            channel.Background = (Brush)new BrushConverter().ConvertFrom("#FF0B004B");
                            channel.IsReceiving = false;
                        }
                        else
                        {
                            channel.Background = new SolidColorBrush(Colors.DarkGray);
                            channel.IsReceiving = false;
                        }
                    });

                    channel.VoiceChannel = null;
                }
            }
        }

        private void HandleVoiceResponse(GRP_VCH_RSP response)
        {
            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (system.IsDvm)
                    continue;

                IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                if (channel.PttState && response.Status == (int)ResponseType.GRANT && response.Channel != null && response.SrcId == system.Rid && response.DstId == cpgChannel.Tgid)
                {
                    channel.VoiceChannel = response.Channel;
                }
                else if (response.Status == (int)ResponseType.GRANT && response.SrcId != system.Rid && response.DstId == cpgChannel.Tgid)
                {
                    channel.VoiceChannel = response.Channel;

                    string alias = string.Empty;

                    try
                    {
                        alias = AliasTools.GetAliasByRid(system.RidAlias, int.Parse(response.SrcId));
                    }
                    catch (Exception) { }

                    if (alias.IsNullOrEmpty())
                        channel.LastSrcId = "Last SRC: " + response.SrcId;
                    else
                        channel.LastSrcId = "Last: " + alias;

                    Dispatcher.Invoke(() =>
                    {
                        channel.Background = (Brush)new BrushConverter().ConvertFrom("#FF00BC48");
                        channel.IsReceiving = true;
                    });
                }
                else if ((channel.HoldState || channel.PageState) && response.Status == (int)ResponseType.GRANT && response.Channel != null && response.SrcId == system.Rid && response.DstId == cpgChannel.Tgid)
                {
                    channel.VoiceChannel = response.Channel;
                }
                else
                {
                    //Dispatcher.Invoke(() =>
                    //{
                    //    if (channel.IsSelected)
                    //        channel.Background = new SolidColorBrush(Colors.DodgerBlue);
                    //    else
                    //        channel.Background = new SolidColorBrush(Colors.Gray);
                    //});

                    //channel.VoiceChannel = null;
                    //_stopSending = true;
                }
            }
        }

        private void ChannelBox_HoldChannelButtonClicked(object sender, ChannelBox e)
        {
            if (e.SystemName == PLAYBACKSYS || e.ChannelName == PLAYBACKCHNAME || e.DstId == PLAYBACKTG)
                return;

            Codeplug.System system = Codeplug.GetSystemForChannel(e.ChannelName);
            Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(e.ChannelName);

            if (system.IsDvm)
                return;

            IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);
        }

        private void ChannelBox_PageButtonClicked(object sender, ChannelBox e)
        {
            if (e.SystemName == PLAYBACKSYS || e.ChannelName == PLAYBACKCHNAME || e.DstId == PLAYBACKTG)
                return;

            Codeplug.System system = Codeplug.GetSystemForChannel(e.ChannelName);
            Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(e.ChannelName);

            if (!system.IsDvm)
            {

                IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                if (e.PageState)
                {
                    GRP_VCH_REQ request = new GRP_VCH_REQ
                    {
                        SrcId = system.Rid,
                        DstId = cpgChannel.Tgid,
                        Site = system.Site
                    };

                    handler.SendMessage(request.GetData());
                }
                else
                {
                    GRP_VCH_RLS release = new GRP_VCH_RLS
                    {
                        SrcId = system.Rid,
                        DstId = cpgChannel.Tgid,
                        Channel = e.VoiceChannel,
                        Site = system.Site
                    };

                    handler.SendMessage(release.GetData());
                    e.VoiceChannel = null;
                }
            }
            else
            {
                PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);
                if (e.PageState)
                {
                    handler.SendP25TDU(UInt32.Parse(system.Rid), UInt32.Parse(cpgChannel.Tgid), true);
                }
                else
                {
                    handler.SendP25TDU(UInt32.Parse(system.Rid), UInt32.Parse(cpgChannel.Tgid), false);
                }
            }
        }

        private void ChannelBox_PTTButtonClicked(object sender, ChannelBox e)
        {
            if (e.SystemName == PLAYBACKSYS || e.ChannelName == PLAYBACKCHNAME || e.DstId == PLAYBACKTG)
                return;

            Codeplug.System system = Codeplug.GetSystemForChannel(e.ChannelName);
            Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(e.ChannelName);

            if (!system.IsDvm)
            {
                IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                if (!e.IsSelected)
                    return;

                if (e.PttState)
                {
                    GRP_VCH_REQ request = new GRP_VCH_REQ
                    {
                        SrcId = system.Rid,
                        DstId = cpgChannel.Tgid,
                        Site = system.Site
                    };

                    handler.SendMessage(request.GetData());
                }
                else
                {
                    GRP_VCH_RLS release = new GRP_VCH_RLS
                    {
                        SrcId = system.Rid,
                        DstId = cpgChannel.Tgid,
                        Channel = e.VoiceChannel,
                        Site = system.Site
                    };

                    handler.SendMessage(release.GetData());
                    e.VoiceChannel = null;
                }
            } else
            {
                PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                if (!e.IsSelected)
                    return;

                FneUtils.Memset(e.mi, 0x00, P25Defines.P25_MI_LENGTH);

                uint srcId = UInt32.Parse(system.Rid);
                uint dstId = UInt32.Parse(cpgChannel.Tgid);

                if (e.PttState)
                {
                    e.txStreamId = handler.NewStreamId();

                    Console.WriteLine("sending grant demand " + dstId);
                    handler.SendP25TDU(srcId, dstId, true);
                }
                else
                {
                    Console.WriteLine("sending terminator " + dstId);
                    handler.SendP25TDU(srcId, dstId, false);
                }
            }
        }

        private void ChannelBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode || !(sender is UIElement element)) return;

            _draggedElement = element;
            _startPoint = e.GetPosition(ChannelsCanvas);
            _offsetX = _startPoint.X - Canvas.GetLeft(_draggedElement);
            _offsetY = _startPoint.Y - Canvas.GetTop(_draggedElement);
            _isDragging = true;

            element.CaptureMouse();
        }

        private void ChannelBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isEditMode || !_isDragging || _draggedElement == null) return;

            Point currentPosition = e.GetPosition(ChannelsCanvas);

            // Calculate the new position with snapping to the grid
            double newLeft = Math.Round((currentPosition.X - _offsetX) / GridSize) * GridSize;
            double newTop = Math.Round((currentPosition.Y - _offsetY) / GridSize) * GridSize;

            // Ensure the box stays within canvas bounds
            newLeft = Math.Max(0, Math.Min(newLeft, ChannelsCanvas.ActualWidth - _draggedElement.RenderSize.Width));
            newTop = Math.Max(0, Math.Min(newTop, ChannelsCanvas.ActualHeight - _draggedElement.RenderSize.Height));

            // Apply snapped position
            Canvas.SetLeft(_draggedElement, newLeft);
            Canvas.SetTop(_draggedElement, newTop);

            // Save the new position if it's a ChannelBox
            if (_draggedElement is ChannelBox channelBox)
            {
                _settingsManager.UpdateChannelPosition(channelBox.ChannelName, newLeft, newTop);
            }

            AdjustCanvasHeight();
        }

        private void ChannelBox_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode || !_isDragging || _draggedElement == null) return;

            _isDragging = false;
            _draggedElement.ReleaseMouseCapture();
            _draggedElement = null;
        }

        private void SystemStatusBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => ChannelBox_MouseLeftButtonDown(sender, e);
        private void SystemStatusBox_MouseMove(object sender, MouseEventArgs e) => ChannelBox_MouseMove(sender, e);

        private void SystemStatusBox_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode) return;

            if (sender is SystemStatusBox systemStatusBox)
            {
                double x = Canvas.GetLeft(systemStatusBox);
                double y = Canvas.GetTop(systemStatusBox);
                _settingsManager.SystemStatusPositions[systemStatusBox.SystemName] = new ChannelPosition { X = x, Y = y };

                ChannelBox_MouseRightButtonDown(sender, e);

                AdjustCanvasHeight();
            }
        }

        private void ToggleEditMode_Click(object sender, RoutedEventArgs e)
        {
            isEditMode = !isEditMode;
            var menuItem = (MenuItem)sender;
            menuItem.Header = isEditMode ? "Disable Edit Mode" : "Enable Edit Mode";
            UpdateEditModeForWidgets();
        }

        private void UpdateEditModeForWidgets()
        {
            foreach (var child in ChannelsCanvas.Children)
            {
                if (child is AlertTone alertTone)
                {
                    alertTone.IsEditMode = isEditMode;
                }

                if (child is ChannelBox channelBox)
                {
                    channelBox.IsEditMode = isEditMode;
                }
            }
        }

        private void AddAlertTone_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*",
                Title = "Select Alert Tone"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string alertFilePath = openFileDialog.FileName;
                var alertTone = new AlertTone(alertFilePath)
                {
                    IsEditMode = isEditMode
                };

                alertTone.OnAlertTone += SendAlertTone;

                if (_settingsManager.AlertTonePositions.TryGetValue(alertFilePath, out var position))
                {
                    Canvas.SetLeft(alertTone, position.X);
                    Canvas.SetTop(alertTone, position.Y);
                }
                else
                {
                    Canvas.SetLeft(alertTone, 20);
                    Canvas.SetTop(alertTone, 20);
                }

                alertTone.MouseRightButtonUp += AlertTone_MouseRightButtonUp;

                ChannelsCanvas.Children.Add(alertTone);
                _settingsManager.UpdateAlertTonePaths(alertFilePath);

                AdjustCanvasHeight();
            }
        }

        private void AlertTone_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode) return;

            if (sender is AlertTone alertTone)
            {
                double x = Canvas.GetLeft(alertTone);
                double y = Canvas.GetTop(alertTone);
                _settingsManager.UpdateAlertTonePosition(alertTone.AlertFilePath, x, y);

                AdjustCanvasHeight();
            }
        }

        private void AdjustCanvasHeight()
        {
            double maxBottom = 0;

            foreach (UIElement child in ChannelsCanvas.Children)
            {
                double childBottom = Canvas.GetTop(child) + child.RenderSize.Height;
                if (childBottom > maxBottom)
                {
                    maxBottom = childBottom;
                }
            }

            ChannelsCanvas.Height = maxBottom + 150;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_settingsManager.LastCodeplugPath) && File.Exists(_settingsManager.LastCodeplugPath))
            {
                LoadCodeplug(_settingsManager.LastCodeplugPath);
                //LoadToneSets();
            }
            else
            {
                GenerateChannelWidgets();
            }
        }

        private async Task PlayTone(string ToneA, string ToneB)
        {
            var selectedChannels = _selectedChannelsManager.GetSelectedChannels();

            // Check if any selected channel has PageState = true
            if (!selectedChannels.Any(ch => ch.PageState))
            {
                // No channels with active page state - do nothing
                return;
            }

            foreach (ChannelBox channel in selectedChannels)
            {
                if (!channel.PageState)
                    continue; // skip channels without page state

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                ToneGenerator generator = new ToneGenerator();

                double toneADuration = 1.0;
                double toneBDuration = 3.0;

                byte[] toneA = generator.GenerateTone(double.Parse(ToneA), toneADuration);
                byte[] toneB = generator.GenerateTone(double.Parse(ToneB), toneBDuration);

                byte[] combinedAudio = new byte[toneA.Length + toneB.Length];
                Buffer.BlockCopy(toneA, 0, combinedAudio, 0, toneA.Length);
                Buffer.BlockCopy(toneB, 0, combinedAudio, toneA.Length, toneB.Length);

                int chunkSize = system.IsDvm ? 320 : 1600;
                int totalChunks = (combinedAudio.Length + chunkSize - 1) / chunkSize;

                _audioManager.AddTalkgroupStream(cpgChannel.Tgid, combinedAudio);

                await Task.Run(() =>
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        int offset = i * chunkSize;
                        int size = Math.Min(chunkSize, combinedAudio.Length - offset);

                        byte[] chunk = new byte[chunkSize];
                        Buffer.BlockCopy(combinedAudio, offset, chunk, 0, size);

                        if (!system.IsDvm)
                        {
                            IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                            AudioPacket voicePacket = new AudioPacket
                            {
                                Data = chunk,
                                VoiceChannel = new VoiceChannel
                                {
                                    Frequency = channel.VoiceChannel,
                                    DstId = cpgChannel.Tgid,
                                    SrcId = system.Rid,
                                    Site = system.Site
                                },
                                Site = system.Site,
                                LopServerVocode = true
                            };

                            handler.SendMessage(voicePacket.GetData());
                        }
                        else
                        {
                            PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                            if (chunk.Length == 320)
                            {
                                P25EncodeAudioFrame(chunk, handler, channel, cpgChannel, system);
                            }
                        }
                    }
                });

                double totalDurationMs = (toneADuration + toneBDuration) * 1000 + 750;
                await Task.Delay((int)totalDurationMs);

                if (!system.IsDvm)
                {
                    IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                    GRP_VCH_RLS release = new GRP_VCH_RLS
                    {
                        SrcId = system.Rid,
                        DstId = cpgChannel.Tgid,
                        Channel = channel.VoiceChannel,
                        Site = system.Site
                    };

                    handler.SendMessage(release.GetData());
                }
                else
                {
                    PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                    await Task.Delay(4000);
                    handler.SendP25TDU(uint.Parse(system.Rid), uint.Parse(cpgChannel.Tgid), false);
                }

                Dispatcher.Invoke(() =>
                {
                    channel.PageSelectButton.Background = channel.grayGradient;
                });
            }
        }

        private async void OnHoldTimerElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (!system.IsDvm)
                {

                    IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                    if (channel.HoldState && !channel.IsReceiving && !channel.PttState && !channel.PageState)
                    {
                        //Task.Factory.StartNew(async () =>
                        //{
                        Console.WriteLine("Sending channel hold beep");

                        Dispatcher.Invoke(() => { channel.PttButton.Background = channel.redGradient; });

                        GRP_VCH_REQ req = new GRP_VCH_REQ
                        {
                            SrcId = system.Rid,
                            DstId = cpgChannel.Tgid,
                            Site = system.Site
                        };

                        handler.SendMessage(req.GetData());

                        await Task.Delay(1000);

                        SendAlertTone("hold.wav", true);
                        // });
                    }
                } else
                {
                    PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                    if (channel.HoldState && !channel.IsReceiving && !channel.PttState && !channel.PageState)
                    {
                        handler.SendP25TDU(UInt32.Parse(system.Rid), UInt32.Parse(cpgChannel.Tgid), true);
                        await Task.Delay(1000);

                        SendAlertTone("hold.wav", true);
                    }
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _settingsManager.SaveSettings();
            base.OnClosing(e);
            Application.Current.Shutdown();
        }

        private void ClearEmergency_Click(object sender, RoutedEventArgs e)
        {
            _emergencyAlertPlayback.Stop();
            _flashingManager.Stop();

            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                channel.Emergency = false;
            }
        }

        private void btnAlert1_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => {
                SendAlertTone("alert1.wav");
            });
        }

        private void btnAlert2_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SendAlertTone("alert2.wav");
            });
        }

        private void btnAlert3_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SendAlertTone("alert3.wav");
            });
        }

        private async void btnGlobalPtt_Click(object sender, RoutedEventArgs e)
        {
            if (globalPttState)
                await Task.Delay(500);

            globalPttState = !globalPttState;

            foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (!system.IsDvm)
                {
                    IPeer handler = _webSocketManager.GetWebSocketHandler(system.Name);

                    if (!channel.IsSelected)
                        continue;

                    if (globalPttState)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            btnGlobalPtt.Background = channel.redGradient;
                        });


                        GRP_VCH_REQ request = new GRP_VCH_REQ
                        {
                            SrcId = system.Rid,
                            DstId = cpgChannel.Tgid,
                            Site = system.Site
                        };

                        channel.PttState = true;

                        handler.SendMessage(request.GetData());
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            btnGlobalPtt.Background = channel.grayGradient;
                        });

                        GRP_VCH_RLS release = new GRP_VCH_RLS
                        {
                            SrcId = system.Rid,
                            DstId = cpgChannel.Tgid,
                            Site = system.Site
                        };

                        channel.PttState = false;

                        handler.SendMessage(release.GetData());
                    }
                } else
                {
                    PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                    channel.txStreamId = handler.NewStreamId();

                    if (globalPttState)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            btnGlobalPtt.Background = channel.redGradient;
                            channel.PttState = true;
                        });

                        handler.SendP25TDU(UInt32.Parse(system.Rid), UInt32.Parse(cpgChannel.Tgid), true);
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            btnGlobalPtt.Background = channel.grayGradient;
                            channel.PttState = false;
                        });

                        handler.SendP25TDU(UInt32.Parse(system.Rid), UInt32.Parse(cpgChannel.Tgid), false);
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) { /* sub */ }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelBox channel in ChannelsCanvas.Children.OfType<ChannelBox>())
            {
                if (channel.SystemName == PLAYBACKSYS || channel.ChannelName == PLAYBACKCHNAME || channel.DstId == PLAYBACKTG)
                    continue;

                Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                if (!channel.IsSelected)
                {
                    channel.IsSelected = true;

                    channel.Background = channel.IsSelected ? (Brush)new BrushConverter().ConvertFrom("#FF0B004B") : Brushes.Gray;

                    if (channel.IsSelected)
                    {
                        _selectedChannelsManager.AddSelectedChannel(channel);
                    }
                    else
                    {
                        _selectedChannelsManager.RemoveSelectedChannel(channel);
                    }
                }
            }
        }

        /// <summary>
        /// Helper to encode and transmit PCM audio as P25 IMBE frames.
        /// </summary>
        private void P25EncodeAudioFrame(byte[] pcm, PeerSystem handler, ChannelBox channel, Codeplug.Channel cpgChannel, Codeplug.System system)
        {
            bool encryptCall = true; // TODO: make this dynamic somewhere?

            if (channel.p25N > 17)
                channel.p25N = 0;
            if (channel.p25N == 0)
                FneUtils.Memset(channel.netLDU1, 0, 9 * 25);
            if (channel.p25N == 9)
                FneUtils.Memset(channel.netLDU2, 0, 9 * 25);

            // Log.Logger.Debug($"BYTE BUFFER {FneUtils.HexDump(pcm)}");

            //// pre-process: apply gain to PCM audio frames
            //if (Program.Configuration.TxAudioGain != 1.0f)
            //{
            //    BufferedWaveProvider buffer = new BufferedWaveProvider(waveFormat);
            //    buffer.AddSamples(pcm, 0, pcm.Length);

            //    VolumeWaveProvider16 gainControl = new VolumeWaveProvider16(buffer);
            //    gainControl.Volume = Program.Configuration.TxAudioGain;
            //    gainControl.Read(pcm, 0, pcm.Length);
            //}

            int smpIdx = 0;
            short[] samples = new short[FneSystemBase.MBE_SAMPLES_LENGTH];
            for (int pcmIdx = 0; pcmIdx < pcm.Length; pcmIdx += 2)
            {
                samples[smpIdx] = (short)((pcm[pcmIdx + 1] << 8) + pcm[pcmIdx + 0]);
                smpIdx++;
            }

            // Convert to floats
            float[] fSamples = AudioConverter.PcmToFloat(samples);

            // Convert to signal
            DiscreteSignal signal = new DiscreteSignal(8000, fSamples, true);

            // Log.Logger.Debug($"SAMPLE BUFFER {FneUtils.HexDump(samples)}");

            // encode PCM samples into IMBE codewords
            byte[] imbe = new byte[FneSystemBase.IMBE_BUF_LEN];


            int tone = 0;

            if (true) // TODO: Disable/enable detection
            {
                tone = channel.toneDetector.Detect(signal);
            }
            if (tone > 0)
            {
                MBEToneGenerator.IMBEEncodeSingleTone((ushort)tone, imbe);
                Console.WriteLine($"({system.Name}) P25D: {tone} HZ TONE DETECT");
            }
            else
            {
#if WIN32
                if (channel.extFullRateVocoder == null)
                    channel.extFullRateVocoder = new AmbeVocoder(true);

                channel.extFullRateVocoder.encode(samples, out imbe);
#else
                if (channel.encoder == null)
                    channel.encoder = new MBEEncoder(MBE_MODE.IMBE_88BIT);

                channel.encoder.encode(samples, imbe);
#endif
            }
            // Log.Logger.Debug($"IMBE {FneUtils.HexDump(imbe)}");

            if (encryptCall && cpgChannel.GetAlgoId() != 0 && cpgChannel.GetKeyId() != 0)
            {
                // initial HDU MI
                if (channel.p25N == 0)
                {
                    if (channel.mi.All(b => b == 0))
                    {
                        Random random = new Random();

                        for (int i = 0; i < P25Defines.P25_MI_LENGTH; i++)
                        {
                            channel.mi[i] = (byte)random.Next(0x00, 0x100);
                        }
                    }

                    channel.crypter.Prepare(cpgChannel.GetAlgoId(), cpgChannel.GetKeyId(), ProtocolType.P25Phase1, channel.mi);
                }

                // crypto time
                channel.crypter.Process(imbe, channel.p25N < 9U ? P25Crypto.FrameType.LDU1 : P25Crypto.FrameType.LDU2, 0);

                // last block of LDU2, prepare a new MI
                if (channel.p25N == 17U)
                {
                    P25Crypto.CycleP25Lfsr(channel.mi);
                    channel.crypter.Prepare(cpgChannel.GetAlgoId(), cpgChannel.GetKeyId(), ProtocolType.P25Phase1, channel.mi);
                }
            }

            // fill the LDU buffers appropriately
            switch (channel.p25N)
            {
                // LDU1
                case 0:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 10, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 1:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 26, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 2:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 55, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 3:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 80, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 4:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 105, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 5:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 130, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 6:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 155, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 7:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 180, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 8:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU1, 204, FneSystemBase.IMBE_BUF_LEN);
                    break;

                // LDU2
                case 9:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 10, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 10:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 26, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 11:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 55, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 12:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 80, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 13:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 105, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 14:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 130, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 15:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 155, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 16:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 180, FneSystemBase.IMBE_BUF_LEN);
                    break;
                case 17:
                    Buffer.BlockCopy(imbe, 0, channel.netLDU2, 204, FneSystemBase.IMBE_BUF_LEN);
                    break;
            }

            uint srcId = UInt32.Parse(system.Rid);
            uint dstId = UInt32.Parse(cpgChannel.Tgid);

            FnePeer peer = handler.peer;
            RemoteCallData callData = new RemoteCallData()
            {
                SrcId = srcId,
                DstId = dstId,
                LCO = P25Defines.LC_GROUP
            };

            // send P25 LDU1
            if (channel.p25N == 8U)
            {
                ushort pktSeq = 0;
                if (channel.p25SeqNo == 0U)
                    pktSeq = peer.pktSeq(true);
                else
                    pktSeq = peer.pktSeq();

                //Console.WriteLine($"({channel.SystemName}) P25D: Traffic *VOICE FRAME    * PEER {handler.PeerId} SRC_ID {srcId} TGID {dstId} [STREAM ID {channel.txStreamId}]");

                byte[] payload = new byte[200];
                handler.CreateNewP25MessageHdr((byte)P25DUID.LDU1, callData, ref payload, cpgChannel.GetAlgoId(), cpgChannel.GetKeyId(), channel.mi);
                handler.CreateP25LDU1Message(channel.netLDU1, ref payload, srcId, dstId);

                peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), payload, pktSeq, channel.txStreamId);
            }

            // send P25 LDU2
            if (channel.p25N == 17U)
            {
                ushort pktSeq = 0;
                if (channel.p25SeqNo == 0U)
                    pktSeq = peer.pktSeq(true);
                else
                    pktSeq = peer.pktSeq();

                //Console.WriteLine($"({channel.SystemName}) P25D: Traffic *VOICE FRAME    * PEER {handler.PeerId} SRC_ID {srcId} TGID {dstId} [STREAM ID {channel.txStreamId}]");

                byte[] payload = new byte[200];
                handler.CreateNewP25MessageHdr((byte)P25DUID.LDU2, callData, ref payload, cpgChannel.GetAlgoId(), cpgChannel.GetKeyId(), channel.mi);
                handler.CreateP25LDU2Message(channel.netLDU2, ref payload, new CryptoParams { AlgId = cpgChannel.GetAlgoId(), KeyId = cpgChannel.GetKeyId(), Mi = channel.mi });

                peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), payload, pktSeq, channel.txStreamId);
            }

            channel.p25SeqNo++;
            channel.p25N++;
        }

        /// <summary>
        /// Helper to decode and playback P25 IMBE frames as PCM audio.
        /// </summary>
        /// <param name="ldu"></param>
        /// <param name="e"></param>
        private void P25DecodeAudioFrame(byte[] ldu, P25DataReceivedEvent e, PeerSystem system, ChannelBox channel, bool emergency = false, P25Crypto.FrameType frameType = P25Crypto.FrameType.LDU1)
        {
            try
            {
                // decode 9 IMBE codewords into PCM samples
                for (int n = 0; n < 9; n++)
                {
                    byte[] imbe = new byte[FneSystemBase.IMBE_BUF_LEN];
                    switch (n)
                    {
                        case 0:
                            Buffer.BlockCopy(ldu, 10, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 1:
                            Buffer.BlockCopy(ldu, 26, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 2:
                            Buffer.BlockCopy(ldu, 55, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 3:
                            Buffer.BlockCopy(ldu, 80, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 4:
                            Buffer.BlockCopy(ldu, 105, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 5:
                            Buffer.BlockCopy(ldu, 130, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 6:
                            Buffer.BlockCopy(ldu, 155, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 7:
                            Buffer.BlockCopy(ldu, 180, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                        case 8:
                            Buffer.BlockCopy(ldu, 204, imbe, 0, FneSystemBase.IMBE_BUF_LEN);
                            break;
                    }

                    //Log.Logger.Debug($"Decoding IMBE buffer: {FneUtils.HexDump(imbe)}");

                    short[] samples = new short[FneSystemBase.MBE_SAMPLES_LENGTH];

                    channel.crypter.Process(imbe, frameType, n);

#if WIN32
                    if (channel.extFullRateVocoder == null)
                        channel.extFullRateVocoder = new AmbeVocoder(true);

                    channel.p25Errs = channel.extFullRateVocoder.decode(imbe, out samples);
#else

                    channel.p25Errs = channel.decoder.decode(imbe, samples);
#endif

                    if (emergency)
                    {
                        if (!channel.Emergency)
                        {
                            Task.Run(() =>
                            {
                                HandleEmergencyAlarmResponse(new EMRG_ALRM_RSP
                                {
                                    SrcId = e.SrcId.ToString(),
                                    DstId = e.DstId.ToString()
                                });
                            });
                        }
                    }

                    if (samples != null)
                    {
                        //Log.Logger.Debug($"({Config.Name}) P25D: Traffic *VOICE FRAME    * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} VC{n} ERRS {errs} [STREAM ID {e.StreamId}]");
                        //Log.Logger.Debug($"IMBE {FneUtils.HexDump(imbe)}");
                        //Console.WriteLine($"SAMPLE BUFFER {FneUtils.HexDump(samples)}");

                        int pcmIdx = 0;
                        byte[] pcmData = new byte[samples.Length * 2];
                        for (int i = 0; i < samples.Length; i++)
                        {
                            pcmData[pcmIdx] = (byte)(samples[i] & 0xFF);
                            pcmData[pcmIdx + 1] = (byte)((samples[i] >> 8) & 0xFF);
                            pcmIdx += 2;
                        }

                        _audioManager.AddTalkgroupStream(e.DstId.ToString(), pcmData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio Decode Exception: {ex.Message}");
            }
        }

        private uint GetUniqueRid(string ridString)
        {
            uint rid;

            // Try to parse the RID, default to 1000 if parsing fails
            if (!UInt32.TryParse(ridString, out rid))
            {
                rid = 1000;
            }

            // Ensure uniqueness by incrementing if needed
            while (usedRids.Contains(rid))
            {
                rid++;
            }

            // Store the new unique RID
            usedRids.Add(rid);

            return rid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void KeyResponseReceived(KeyResponseEvent e)
        {
            //Console.WriteLine($"Message ID: {e.KmmKey.MessageId}");
            //Console.WriteLine($"Decrypt Info Format: {e.KmmKey.DecryptInfoFmt}");
            //Console.WriteLine($"Algorithm ID: {e.KmmKey.AlgId}");
            //Console.WriteLine($"Key ID: {e.KmmKey.KeyId}");
            //Console.WriteLine($"Keyset ID: {e.KmmKey.KeysetItem.KeysetId}");
            //Console.WriteLine($"Keyset Alg ID: {e.KmmKey.KeysetItem.AlgId}");
            //Console.WriteLine($"Keyset Key Length: {e.KmmKey.KeysetItem.KeyLength}");
            //Console.WriteLine($"Number of Keys: {e.KmmKey.KeysetItem.Keys.Count}");

            foreach (var key in e.KmmKey.KeysetItem.Keys)
            {
                //Console.WriteLine($"  Key Format: {key.KeyFormat}");
                //Console.WriteLine($"  SLN: {key.Sln}");
                //Console.WriteLine($"  Key ID: {key.KeyId}");
                //Console.WriteLine($"  Key Data: {BitConverter.ToString(key.GetKey())}");

                Dispatcher.Invoke(() =>
                {
                    foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
                    {
                        Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                        Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                        if (!system.IsDvm)
                            continue;

                        PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                        if (cpgChannel.GetKeyId() != 0 && cpgChannel.GetAlgoId() != 0)
                            channel.crypter.AddKey(key.KeyId, e.KmmKey.KeysetItem.AlgId, key.GetKey());
                    }
                });
            }
        }

        private void KeyStatus_Click(object sender, RoutedEventArgs e)
        {
            KeyStatusWindow keyStatus = new KeyStatusWindow(Codeplug, this);
            keyStatus.Show();
        }

        /// <summary>
        /// Event handler used to process incoming P25 data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void P25DataReceived(P25DataReceivedEvent e, DateTime pktTime)
        {
            uint sysId = (uint)((e.Data[11U] << 8) | (e.Data[12U] << 0));
            uint netId = FneUtils.Bytes3ToUInt32(e.Data, 16);
            byte control = e.Data[14U];

            byte len = e.Data[23];
            byte[] data = new byte[len];
            for (int i = 24; i < len; i++)
                data[i - 24] = e.Data[i];

            Dispatcher.Invoke(() =>
            {
                foreach (ChannelBox channel in _selectedChannelsManager.GetSelectedChannels())
                {
                    Codeplug.System system = Codeplug.GetSystemForChannel(channel.ChannelName);
                    Codeplug.Channel cpgChannel = Codeplug.GetChannelByName(channel.ChannelName);

                    bool isEmergency = false;
                    bool encrypted = false;

                    if (!system.IsDvm)
                        continue;

                    PeerSystem handler = _fneSystemManager.GetFneSystem(system.Name);

                    if (!channel.IsEnabled)
                        continue;

                    if (cpgChannel.Tgid != e.DstId.ToString())
                        continue;

                    if (!systemStatuses.ContainsKey(cpgChannel.Name))
                    {
                        systemStatuses[cpgChannel.Name] = new SlotStatus();
                    }

                    if (channel.decoder == null)
                    {
                        channel.decoder = new MBEDecoder(MBE_MODE.IMBE_88BIT);
                    }

                    SlotStatus slot = systemStatuses[cpgChannel.Name];

                    // if this is an LDU1 see if this is the first LDU with HDU encryption data
                    if (e.DUID == P25DUID.LDU1)
                    {
                        byte frameType = e.Data[180];

                        // get the initial MI and other enc info (bug found by the screeeeeeeeech on initial tx...)
                        if (frameType == P25Defines.P25_FT_HDU_VALID)
                        {
                            channel.algId = e.Data[181];
                            channel.kId = (ushort)((e.Data[182] << 8) | e.Data[183]);
                            Array.Copy(e.Data, 184, channel.mi, 0, P25Defines.P25_MI_LENGTH);

                            channel.crypter.Prepare(channel.algId, channel.kId, P25Crypto.ProtocolType.P25Phase1, channel.mi);

                            encrypted = true;
                        }
                    }

                    // is this a new call stream?
                    if (e.StreamId != slot.RxStreamId && ((e.DUID != P25DUID.TDU) && (e.DUID != P25DUID.TDULC)))
                    {
                        channel.IsReceiving = true;
                        slot.RxStart = pktTime;
                        Console.WriteLine($"({system.Name}) P25D: Traffic *CALL START     * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} [STREAM ID {e.StreamId}]");

                        FneUtils.Memset(channel.mi, 0x00, P25Defines.P25_MI_LENGTH);

                        callHistoryWindow.AddCall(cpgChannel.Name, (int)e.SrcId, (int)e.DstId);
                        callHistoryWindow.ChannelKeyed(cpgChannel.Name, (int)e.SrcId, encrypted);

                        string alias = string.Empty;

                        try
                        {
                            alias = AliasTools.GetAliasByRid(system.RidAlias, (int)e.SrcId);
                        }
                        catch (Exception) { }

                        if (alias.IsNullOrEmpty())
                            channel.LastSrcId = "Last SRC: " + e.SrcId;
                        else
                            channel.LastSrcId = "Last: " + alias;

                        if (channel.algId != P25Defines.P25_ALGO_UNENCRYPT)
                            channel.Background = (Brush)new BrushConverter().ConvertFrom("#ffdeaf0a");
                        else
                            channel.Background = (Brush)new BrushConverter().ConvertFrom("#FF00BC48");
                    }

                    // Is the call over?
                    if (((e.DUID == P25DUID.TDU) || (e.DUID == P25DUID.TDULC)) && (slot.RxType != fnecore.FrameType.TERMINATOR))
                    {
                        channel.IsReceiving = false;
                        TimeSpan callDuration = pktTime - slot.RxStart;
                        Console.WriteLine($"({system.Name}) P25D: Traffic *CALL END       * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} DUR {callDuration} [STREAM ID {e.StreamId}]");
                        channel.Background = (Brush)new BrushConverter().ConvertFrom("#FF0B004B");
                        callHistoryWindow.ChannelUnkeyed(cpgChannel.Name, (int)e.SrcId);
                        return;
                    }

                    if ((channel.algId != cpgChannel.GetAlgoId() || channel.kId != cpgChannel.GetKeyId()) && channel.algId != P25Defines.P25_ALGO_UNENCRYPT)
                        continue;

                    byte[] newMI = new byte[P25Defines.P25_MI_LENGTH];

                    int count = 0;

                    switch (e.DUID)
                    {
                        case P25DUID.LDU1:
                            {
                                // The '62', '63', '64', '65', '66', '67', '68', '69', '6A' records are LDU1
                                if ((data[0U] == 0x62U) && (data[22U] == 0x63U) &&
                                    (data[36U] == 0x64U) && (data[53U] == 0x65U) &&
                                    (data[70U] == 0x66U) && (data[87U] == 0x67U) &&
                                    (data[104U] == 0x68U) && (data[121U] == 0x69U) &&
                                    (data[138U] == 0x6AU))
                                {
                                    // The '62' record - IMBE Voice 1
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 0, 22);
                                    count += 22;

                                    // The '63' record - IMBE Voice 2
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 25, 14);
                                    count += 14;

                                    // The '64' record - IMBE Voice 3 + Link Control
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 50, 17);
                                    byte serviceOptions = data[count + 3];
                                    isEmergency = (serviceOptions & 0x80) == 0x80;
                                    count += 17;

                                    // The '65' record - IMBE Voice 4 + Link Control
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 75, 17);
                                    count += 17;

                                    // The '66' record - IMBE Voice 5 + Link Control
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 100, 17);
                                    count += 17;

                                    // The '67' record - IMBE Voice 6 + Link Control
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 125, 17);
                                    count += 17;

                                    // The '68' record - IMBE Voice 7 + Link Control
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 150, 17);
                                    count += 17;

                                    // The '69' record - IMBE Voice 8 + Link Control
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 175, 17);
                                    count += 17;

                                    // The '6A' record - IMBE Voice 9 + Low Speed Data
                                    Buffer.BlockCopy(data, count, channel.netLDU1, 200, 16);
                                    count += 16;

                                    // decode 9 IMBE codewords into PCM samples
                                    P25DecodeAudioFrame(channel.netLDU1, e, handler, channel, isEmergency);
                                }
                            }
                            break;
                        case P25DUID.LDU2:
                            {
                                // The '6B', '6C', '6D', '6E', '6F', '70', '71', '72', '73' records are LDU2
                                if ((data[0U] == 0x6BU) && (data[22U] == 0x6CU) &&
                                    (data[36U] == 0x6DU) && (data[53U] == 0x6EU) &&
                                    (data[70U] == 0x6FU) && (data[87U] == 0x70U) &&
                                    (data[104U] == 0x71U) && (data[121U] == 0x72U) &&
                                    (data[138U] == 0x73U))
                                {
                                    // The '6B' record - IMBE Voice 10
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 0, 22);
                                    count += 22;

                                    // The '6C' record - IMBE Voice 11
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 25, 14);
                                    count += 14;

                                    // The '6D' record - IMBE Voice 12 + Encryption Sync
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 50, 17);
                                    newMI[0] = data[count + 1];
                                    newMI[1] = data[count + 2];
                                    newMI[2] = data[count + 3];
                                    count += 17;

                                    // The '6E' record - IMBE Voice 13 + Encryption Sync
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 75, 17);
                                    newMI[3] = data[count + 1];
                                    newMI[4] = data[count + 2];
                                    newMI[5] = data[count + 3];
                                    count += 17;

                                    // The '6F' record - IMBE Voice 14 + Encryption Sync
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 100, 17);
                                    newMI[6] = data[count + 1];
                                    newMI[7] = data[count + 2];
                                    newMI[8] = data[count + 3];
                                    count += 17;

                                    // The '70' record - IMBE Voice 15 + Encryption Sync
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 125, 17);
                                    channel.algId = data[count + 1];                                    // Algorithm ID
                                    channel.kId = (ushort)((data[count + 2] << 8) | data[count + 3]);   // Key ID
                                    count += 17;

                                    // The '71' record - IMBE Voice 16 + Encryption Sync
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 150, 17);
                                    count += 17;

                                    // The '72' record - IMBE Voice 17 + Encryption Sync
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 175, 17);
                                    count += 17;

                                    // The '73' record - IMBE Voice 18 + Low Speed Data
                                    Buffer.BlockCopy(data, count, channel.netLDU2, 200, 16);
                                    count += 16;

                                    if (channel.p25Errs > 0) // temp, need to actually get errors I guess
                                        P25Crypto.CycleP25Lfsr(channel.mi);
                                    else
                                        Array.Copy(newMI, channel.mi, P25Defines.P25_MI_LENGTH);

                                    // decode 9 IMBE codewords into PCM samples
                                    P25DecodeAudioFrame(channel.netLDU2, e, handler, channel, isEmergency, P25Crypto.FrameType.LDU2);
                                }
                            }
                            break;
                    }

                    if (channel.mi != null)
                        channel.crypter.Prepare(channel.algId, channel.kId, P25Crypto.ProtocolType.P25Phase1, channel.mi);

                    slot.RxRFS = e.SrcId;
                    slot.RxType = e.FrameType;
                    slot.RxTGId = e.DstId;
                    slot.RxTime = pktTime;
                    slot.RxStreamId = e.StreamId;

                }
            });
        }

        private void CallHist_Click(object sender, RoutedEventArgs e)
        {
            callHistoryWindow.Show();
        }
    }
}
