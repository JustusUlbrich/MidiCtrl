using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MidiCtrl
{
    class MyAudioSession : INotifyPropertyChanged
    {
        public AudioSessionControl AudioSessionControl { get; set; }

        // VM
        public string FriendlyName { get; set; }
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

    class AudioContextEnumerator
    {
        public static List<MyAudioSession> GetAudioSessions()
        {
            var sessionList = new List<MyAudioSession>();

            var enumerator = new MMDeviceEnumerator();
            
            var collection = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            foreach (var endpoint in collection)
            {
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
                var FriendlyName = Process.GetProcessById(processID).ProcessName;
                audioSession.FriendlyName = FriendlyName;

                if (processID > 0 && !sessionList.Exists(a => a.AudioSessionControl.GetProcessID == processID))
                {
                    audioSession.AudioSessionControl.RegisterEventClient(new MyEventHandler(audioSession));
                    sessionList.Add(audioSession);
                }

            }
        }
    }
}
