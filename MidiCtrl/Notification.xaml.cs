using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace MidiCtrl
{
    /// <summary>
    /// Interaction logic for Notification.xaml
    /// </summary>
    public partial class Notification : Window
    {
        public ObservableCollection<MyAudioSession> AudioSessions { get; set; }

        private Dictionary<int, Timer> closeTimers = new Dictionary<int, Timer>();

        public Notification()
        {
            AudioSessions = new ObservableCollection<MyAudioSession>();
            
            InitializeComponent();
            this.Show();
        }

        public void Add(MyAudioSession audioSession, Point screenCorner)
        {
            this.Topmost = true;

            // Adjust current corner
            this.Left = screenCorner.X - this.ActualWidth - 10;
            this.Top = screenCorner.Y - this.ActualHeight - 10;

            Timer timer;
            bool hasTimer = closeTimers.TryGetValue(audioSession.GetHashCode(), out timer);
            if (hasTimer == false)
            {
                AudioSessions.Add(audioSession);

                timer = new Timer(1000);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) => Timer_Elapsed(s, e, audioSession);

                closeTimers[audioSession.GetHashCode()] = timer;
            }

            // Reset Timer
            timer.Stop();
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e, MyAudioSession audioSession)
        {
            ((Timer) sender).Stop();
            closeTimers.Remove(audioSession.GetHashCode());
            this.Dispatcher.Invoke(() => Remove(audioSession));
        }

        private void Remove(MyAudioSession audioSession)
        {
            Console.WriteLine("removing " + audioSession.FriendlyName);
            AudioSessions.Remove(audioSession);
        }
    }
}
