using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiCtrl
{
    // VM
    class MyMidiDevice
    {
        public string Name { get; set; }
        public MidiIn MidiIn { get; set; }
    }

    class MidiEnumerator
    {
        public List<MyMidiDevice> midis = new List<MyMidiDevice>();

        public void InitMidiDevices()
        {
            midis.Clear();

            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                var midi = new MyMidiDevice();
                midi.Name = MidiIn.DeviceInfo(device).ProductName;

                var midiIn = new MidiIn(device);
                midiIn.MessageReceived += midiIn_MessageReceived;
                midiIn.ErrorReceived += midiIn_ErrorReceived;
                midiIn.Start();

                midi.MidiIn = midiIn;

                midis.Add(midi);
            }
        }

        void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.Error.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }
    }
}
