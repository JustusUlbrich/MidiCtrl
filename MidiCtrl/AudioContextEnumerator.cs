using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MidiCtrl
{
    public class MyAudioSession : INotifyPropertyChanged
    {
        public AudioSessionControl AudioSessionControl { get; set; }

        // VM
        public string FriendlyName { get; set; }
        public BitmapSource IconImage { get; set; }

        public int Volume
        {
            get { return (int) (100 * _volume); }
            set {
                _volume = (float) value / 100.0f;
                AudioSessionControl.SimpleAudioVolume.Volume = _volume;
            }
        }

        public void ChangeVolumeExternal( float volume)
        {
            this._volume = volume;
            TriggerPropertyChange("Volume");
        }

        private float _volume = 1.0f;

        public event PropertyChangedEventHandler PropertyChanged;

        public void TriggerPropertyChange(String property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    class MyEventHandler : IAudioSessionEventsHandler
    {
        private MyAudioSession audioSession;

        public MyEventHandler(MyAudioSession audioSession)
        {
            this.audioSession = audioSession;
        }

        public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }

        public void OnDisplayNameChanged(string displayName) { }

        public void OnGroupingParamChanged(ref Guid groupingId) { }

        public void OnIconPathChanged(string iconPath) { }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) { }

        public void OnStateChanged(AudioSessionState state) { }

        public void OnVolumeChanged(float volume, bool isMuted)
        {
            audioSession.ChangeVolumeExternal(volume);
        }
    }

    class MyAudioDevice
    {
        public string FriendlyName { get; set; }
        public string ID { get; set; }
        public bool IsSelected { get; set; }
    }

    class AudioContextEnumerator
    {
        public static List<MyAudioDevice> GetAudioDevices()
        {
            var audioDevices = new List<MyAudioDevice>();

            var enumerator = new MMDeviceEnumerator();
            var collection = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            foreach (var endpoint in collection)
            {
                MyAudioDevice audioDevice = new MyAudioDevice();
                audioDevice.FriendlyName = endpoint.FriendlyName;
                audioDevice.ID = endpoint.ID;

                audioDevices.Add(audioDevice);
            }

            return audioDevices;
        }

        public static List<MyAudioSession> GetAudioSessions(List<String> DeviceIDs)
        {
            var sessionList = new List<MyAudioSession>();

            var enumerator = new MMDeviceEnumerator();
            
            var collection = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            foreach (var endpoint in collection)
            {
                if (DeviceIDs.Contains(endpoint.ID))
                    addSessionsForEndpoint(sessionList, endpoint);
            }
            
            return sessionList;
        }

        private static void addSessionsForEndpoint(List<MyAudioSession> sessionList, MMDevice endpoint)
        {
            var sessions = endpoint.AudioSessionManager.Sessions;
            for (int i = 0; i < sessions.Count; i++)
            {
                var audioSession = new MyAudioSession();
                audioSession.AudioSessionControl = sessions[i];

                var processID = (int)audioSession.AudioSessionControl.GetProcessID;
                var process = Process.GetProcessById(processID);
                var FriendlyName = process.ProcessName;
                audioSession.FriendlyName = FriendlyName;

                if (sessionList.Exists(a => a.AudioSessionControl.GetProcessID == processID))
                    audioSession.FriendlyName += " (@" + endpoint.DeviceFriendlyName + ")";

                if (processID > 0) // && !sessionList.Exists(a => a.AudioSessionControl.GetProcessID == processID))
                {
                    // Extract and store icon
                    try
                    {
                        Icon ico = Icon.ExtractAssociatedIcon(process.MainModule.FileName);

                        BitmapSource iconImage = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        audioSession.IconImage = iconImage;
                    }
                    catch
                    {
                    }

                    // Init Volume
                    audioSession.ChangeVolumeExternal(audioSession.AudioSessionControl.SimpleAudioVolume.Volume);

                    audioSession.AudioSessionControl.RegisterEventClient(new MyEventHandler(audioSession));
                    sessionList.Add(audioSession);
                }

            }
        }
    }
}
