
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace MidiCtrl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MidiEnumerator midiEnumerator = new MidiEnumerator();

        List<string> selectedApps = new List<string>();
        Dictionary<int, string[]> channelToApp = new Dictionary<int, string[]>();

        private System.Threading.Timer minimizeTimer;

        public string SettingsVisibility { get; set; }

        public MainWindow()
        {
            SettingsVisibility = "Collapsed";
            InitializeComponent();
            midiEnumerator.InitMidiDevices();

            this.listBox.ItemsSource = AudioContextEnumerator.GetAudioSessions();
            this.midiList.ItemsSource = midiEnumerator.midis;

            this.midiEnumerator.midis[0].MidiIn.MessageReceived += MidiIn_MessageReceived;

            this.refreshButton.Click += RefreshButton_Click;
            this.settingsButton.Click += SettingsButton_Click;

            for (int i = 1; i <= 16; i++)
            {
                string appString = (string)Properties.Settings.Default["channel" + i];
                if (appString != null && appString.Length > 0)
                {
                    string[] apps = appString.Split(';');
                    channelToApp[i] = apps;
                }
            }

            minimizeTimer = new System.Threading.Timer(_ =>
                Application.Current.Dispatcher.BeginInvoke((Action)(() => this.WindowState = WindowState.Minimized))
            );
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            this.listBox.ItemsSource = AudioContextEnumerator.GetAudioSessions();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.SettingsVisibility = "Visible";
        }

        private void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode != MidiCommandCode.PitchWheelChange)
                return;

            var pwEvent = (PitchWheelChangeEvent)e.MidiEvent;

            var channel = pwEvent.Channel;
            var volume = (float)pwEvent.Pitch / 16383.0f;

            try
            {
                if (selectedApps.Count > 0)
                {
                    connectAppsToChannel(selectedApps, channel);
                    selectedApps.Clear();

                    this.Dispatcher.Invoke(() => this.listBox.SelectedItems.Clear());
                }

                string[] appsOnChannel;
                bool channelHasApps = channelToApp.TryGetValue(channel, out appsOnChannel);
                if (channelHasApps && appsOnChannel.Length > 0)
                {
                    var itemsOnChannel = listBox.Items
                        .Cast<MyAudioSession>()
                        .Where(item => appsOnChannel.Contains(item.FriendlyName));

                    foreach (var item in itemsOnChannel)
                    {
                        item.Volume = (int)(100 * volume);
                    }
                }


                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    this.minimizeTimer.Change(1000, Timeout.Infinite);
                }));
            }
            catch (System.ArgumentOutOfRangeException)
            {
            }
        }

        public void OnAppSelectionChange(object sender, SelectionChangedEventArgs args)
        {
            var hasSelected = (this.listBox.SelectedItems.Count > 0);

            selectedApps.Clear();
            foreach (MyAudioSession selected in this.listBox.SelectedItems)
            {
                selectedApps.Add(selected.FriendlyName);
            }
        }

        private void connectAppsToChannel(List<string> apps, int channel)
        {
            channelToApp[channel] = selectedApps.ToArray();

            string appString = String.Join(";", apps.ToArray());
            Properties.Settings.Default["channel" + channel] = appString;

            Properties.Settings.Default.Save();
        }
    }
}
