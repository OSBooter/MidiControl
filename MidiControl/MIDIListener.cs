﻿using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MidiControl
{
    class MIDIListener
    {
        public List<MidiIn> midiIn = new List<MidiIn>();
        public List<string> midiInStringFeedback = new List<string>();
        public List<string> midiInStringOptions = new List<string>();
        public List<MidiOut> midiOut = new List<MidiOut>();
        public List<string> midiOutStringOptions = new List<string>();
        public Dictionary<string, MidiInCustom> midiInInterface = new Dictionary<string, MidiInCustom>();

        private static MIDIListener _instance;
        private readonly Configuration conf;
        private readonly AudioControl audioControl;
        private readonly OptionsManagment options = OptionsManagment.GetInstance();

        private MidiOutCustom MidiOutdevice;
        private readonly int currentDeviceOut;

        public MIDIListener(Configuration conf)
        {
            new OBSControl();
            audioControl = new AudioControl();

            this.conf = conf;
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                try
                {
                    //if(MidiIn.DeviceInfo(device).ProductName != options.options.MIDIForwardInterface) // Avoid infinite Loopback
                    //{
                        if (!options.options.MIDIInterfaces.Contains(MidiIn.DeviceInfo(device).ProductName))
                        {
                            MidiInCustom MidiIndevice = new MidiInCustom(device);
                            midiInInterface.Add(MidiIn.DeviceInfo(device).ProductName, MidiIndevice);
                            midiInStringFeedback.Add(MidiIn.DeviceInfo(device).ProductName);

                            MidiIndevice.Start();
                            midiIn.Add(MidiIndevice);

                            Debug.WriteLine("MIDI IN Device " + MidiIn.DeviceInfo(device).ProductName);
                        }

                        midiInStringOptions.Add(MidiIn.DeviceInfo(device).ProductName);
                    //}
                }
                catch (NAudio.MmException)
                {
                    // Device already Opened
                }
            }

            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                midiOutStringOptions.Add(MidiOut.DeviceInfo(device).ProductName);

                try
                {
                    if (options.options.MIDIFeedbackEnabled == true)
                    {
                        if(midiInStringFeedback.Contains(MidiOut.DeviceInfo(device).ProductName))
                        {
                            MidiOutCustom MidiOutdevice = new MidiOutCustom(device);
                            midiOut.Add(MidiOutdevice);
                            Debug.WriteLine("MIDI OUT Feedback Device " + MidiOut.DeviceInfo(device).ProductName);
                        }
                    }

                    if (MidiOut.DeviceInfo(device).ProductName == options.options.MIDIForwardInterface)
                    {
                        currentDeviceOut = device;
                        Debug.WriteLine("MIDI OUT Forward Device " + MidiOut.DeviceInfo(device).ProductName);
                    }
                }
                catch (NAudio.MmException)
                {
                    // Device already Opened
                }
            }

            EnableListening();
            _instance = this;
        }

        public void ReleaseAll()
        {
            foreach(MidiIn midi in midiIn)
            {
                try
                {
                    midi.Stop();
                    midi.Dispose();
                }
                catch (NAudio.MmException)
                {
                }
            }
            foreach (MidiOut midi in midiOut)
            {
                try
                {
                    midi.Dispose();
                }
                catch (NAudio.MmException)
                {
                }
            }
        }

        public void EnableListening()
        {
            foreach (KeyValuePair<string, MidiInCustom> entry in midiInInterface)
            {
                entry.Value.MessageReceived += MidiIn_MessageReceived;
            }
        }

        public void DisableListening()
        {
            foreach (KeyValuePair<string, MidiInCustom> entry in midiInInterface)
            {
                entry.Value.MessageReceived -= MidiIn_MessageReceived;
            }
        }

        private void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            Debug.WriteLine("MIDI Signal " + e.MidiEvent.GetType() + " | " + e.MidiEvent.ToString());
            if (options.options.MIDIForwardEnabled == true)
            {
                if(MidiOutdevice == null)
                {
                    MidiOutdevice = new MidiOutCustom(currentDeviceOut);
                }
                MidiOutdevice.Send(e.RawMessage);
            }
            if (options.options.MIDIFeedbackEnabled == true)
            {
                foreach(MidiOutCustom outDevice in midiOut)
                {
                    if(MidiOut.DeviceInfo(outDevice.device).ProductName == MidiIn.DeviceInfo(((MidiInCustom)sender).device).ProductName)
                    {
                        outDevice.Send(e.RawMessage);
                    }
                }
            }

            if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn && ((NoteEvent)e.MidiEvent).Velocity != 0 && 
                options.options.MidiDeviceStopAllSounds == MidiIn.DeviceInfo(((MidiInCustom)sender).device).ProductName && 
                options.options.NoteNumberStopAllSounds == ((NoteEvent)e.MidiEvent).NoteNumber && 
                options.options.ChannelStopAllSounds == ((NoteEvent)e.MidiEvent).Channel)
            {
                this.StopAllSounds();
            }

            foreach (KeyValuePair<string, KeyBindEntry> entry in conf.Config)
            {
                var type = e.MidiEvent.GetType();
                if (type.Name == "ControlChangeEvent" && ((int)((ControlChangeEvent)e.MidiEvent).Controller != entry.Value.NoteNumber ||
                    ((ControlChangeEvent)e.MidiEvent).Channel != entry.Value.Channel || MidiIn.DeviceInfo(((MidiInCustom)sender).device).ProductName != entry.Value.Mididevice)) continue;

                if ((type.Name == "NoteEvent" || type.Name == "NoteOnEvent") && (((NoteEvent)e.MidiEvent).NoteNumber != entry.Value.NoteNumber ||
                    ((NoteEvent)e.MidiEvent).Channel != entry.Value.Channel || MidiIn.DeviceInfo(((MidiInCustom)sender).device).ProductName != entry.Value.Mididevice)) continue;

                if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn && ((NoteEvent)e.MidiEvent).Velocity != 0)
                {
                    foreach (OBSCallBack callback in entry.Value.OBSCallBacksON)
                    {
                        callback.Start();
                    }
                    if(entry.Value.SoundCallBack != null)
                    {
                        audioControl.PlaySound(entry.Value, entry.Value.SoundCallBack.File, entry.Value.SoundCallBack.Device, entry.Value.SoundCallBack.Loop, entry.Value.SoundCallBack.Volume);
                    }
                }
                else if ((e.MidiEvent.CommandCode == MidiCommandCode.NoteOff || e.MidiEvent.CommandCode == MidiCommandCode.NoteOn && ((NoteEvent)e.MidiEvent).Velocity == 0))
                {
                    foreach (OBSCallBack callback in entry.Value.OBSCallBacksOFF)
                    {
                        callback.Start();
                    }
                    if (entry.Value.SoundCallBack != null && entry.Value.SoundCallBack.StopWhenReleased == true)
                    {
                        audioControl.StopSound(entry.Value);
                    }
                }
                else if (e.MidiEvent.CommandCode == MidiCommandCode.ControlChange)
                {
                    if(((ControlChangeEvent)e.MidiEvent).ControllerValue != 0)
                    {
                        foreach (OBSCallBack callback in entry.Value.OBSCallBacksON)
                        {
                            callback.Start();
                        }
                        if (entry.Value.SoundCallBack != null)
                        {
                            audioControl.PlaySound(entry.Value, entry.Value.SoundCallBack.File, entry.Value.SoundCallBack.Device, entry.Value.SoundCallBack.Loop, entry.Value.SoundCallBack.Volume);
                        }
                    }
                    else
                    {
                        foreach (OBSCallBack callback in entry.Value.OBSCallBacksOFF)
                        {
                            callback.Start();
                        }
                        if (entry.Value.SoundCallBack != null && entry.Value.SoundCallBack.StopWhenReleased == true)
                        {
                            audioControl.StopSound(entry.Value);
                        }
                    }
                    foreach (OBSCallBack callback in entry.Value.OBSCallBacksSlider)
                    {
                        double value = Linear2dB((float)((ControlChangeEvent)e.MidiEvent).ControllerValue, 0, 127, -90, 0);
                        callback.Start((float)value);
                    }
                }
            }
        }

        private float Linear2dB(float inp, float ista, float isto, float osta, float osto)
        {
            return osta + (osto - osta) * ((inp - ista) / (isto - ista));
        }


        public void StopAllSounds()
        {
            audioControl.StopAll();
        }

        public static MIDIListener GetInstance()
        {
            return _instance;
        }
    }
}