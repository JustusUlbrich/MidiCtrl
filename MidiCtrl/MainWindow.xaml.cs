using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MidiCtrl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MidiEnumerator midiEnumerator = new MidiEnumerator();

        List<string> allowedDevices = new List<String>();

        List<string> selectedApps = new List<string>();
        Dictionary<int, string[]> channelToApp = new Dictionary<int, string[]>();

        Notification notifications;

        public MainWindow()
        {
            InitializeComponent();
            midiEnumerator.InitMidiDevices();

            this.deviceList.ItemsSource = AudioContextEnumerator.GetAudioDevices();
            InitAllowedDevices();

            this.listBox.ItemsSource = AudioContextEnumerator.GetAudioSessions(allowedDevices);
            this.midiList.ItemsSource = midiEnumerator.midis;

            foreach (var midi in midiEnumerator.midis)
            {
                midi.MidiIn.MessageReceived += MidiIn_MessageReceived;
            }

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

            notifications = new Notification();
        }
        
        private void InitAllowedDevices()
        {
            string deviceString = (string)Properties.Settings.Default["devices"];
            if (deviceString != null && deviceString.Length > 0)
            {
                var storedDevices = deviceString.Split(';').ToList();

                foreach (string ID in storedDevices )
                {
                    var allowedDevice = deviceList.Items
                        .Cast<MyAudioDevice>()
                        .Where(device => ID == device.ID)
                        .DefaultIfEmpty(null)
                        .FirstOrDefault();

                    this.deviceList.SelectedItems.Add(allowedDevice);
                }

                this.allowedDevices = storedDevices;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            this.listBox.ItemsSource = AudioContextEnumerator.GetAudioSessions(allowedDevices);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show Settings
        }

        private Point getCurrentScreenCorner()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(
                new System.Windows.Interop.WindowInteropHelper(this).Handle
            );
            var workingArea = screen.WorkingArea;
            var source = PresentationSource.FromVisual(this);

            var transform = source != null ? source.CompositionTarget.TransformFromDevice : new Matrix();
            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

            return corner;
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

                        this.Dispatcher.BeginInvoke((Action)(() => this.notifications.Add(item, getCurrentScreenCorner())));
                    }
                }
            }
            catch (System.ArgumentOutOfRangeException)
            {
            }
        }

        public void OnAppSelectionChange(object sender, SelectionChangedEventArgs args)
        {
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

        private void DeviceListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            allowedDevices.Clear();
            foreach (MyAudioDevice selected in this.deviceList.SelectedItems)
            {
                allowedDevices.Add(selected.ID);
            }

            string deviceString = String.Join(";", allowedDevices.ToArray());
            Properties.Settings.Default["devices"] = deviceString;
            Properties.Settings.Default.Save();
        }

        // Minimize to system tray when application is minimized.
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) this.Hide();

            base.OnStateChanged(e);
        }

        // Minimize to system tray when application is closed.
        protected override void OnClosing(CancelEventArgs e)
        {
            // setting cancel to true will cancel the close request
            // so the application is not closed
            e.Cancel = true;

            this.Hide();

            base.OnClosing(e);
        }
    }
}
