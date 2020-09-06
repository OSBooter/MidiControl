﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MidiControl
{
    public partial class OptionsGUI : Form
    {
        private readonly OptionsManagment options;
        public OptionsGUI(OptionsManagment options)
        {
            this.options = options;
            InitializeComponent();
            TxtBoxOBSIP.Text = options.options.Ip;
            TxtBoxOBSPassword.Text = options.options.Password;
            ChkBoxAutoConnectStart.Checked = options.options.Autoconnect;
            ChkBoxAutoReconnect.Checked = options.options.AutoReconnect;

            CmbBoxMIDIForward.Text = options.options.MIDIForwardInterface;

            List<string> midiOut = MIDIListener.GetInstance().midiOutStringOptions;
            foreach (string outInterface in midiOut)
            {
                CmbBoxMIDIForward.Items.Add(outInterface);
            }

            List<string> midiIn = MIDIListener.GetInstance().midiInStringOptions;
            foreach (string inInterface in midiIn)
            {
                ChkCmbBoxMIDI.Items.Add(inInterface);
                if(options.options.MIDIInterfaces.Contains(inInterface))
                {
                    ChkCmbBoxMIDI.SetItemChecked(ChkCmbBoxMIDI.Items.IndexOf(inInterface), true);
                }
            }
            ChkBoxMIDIForward.Checked = options.options.MIDIForwardEnabled;
            ChkBoxMIDIFeedback.Checked = options.options.MIDIFeedbackEnabled;

            txtBoxStopAllSoundsDevice.Text = options.options.MidiDeviceStopAllSounds;
            txtBoxStopAllSoundsChannel.Text = options.options.ChannelStopAllSounds.ToString();
            txtBoxStopAllSoundsNote.Text = options.options.NoteNumberStopAllSounds.ToString();
            txtBoxDelay.Text = options.options.Delay.ToString();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            options.options.Ip = TxtBoxOBSIP.Text;
            options.options.Password = TxtBoxOBSPassword.Text;
            options.options.Autoconnect = ChkBoxAutoConnectStart.Checked;
            options.options.AutoReconnect = ChkBoxAutoReconnect.Checked;

            options.options.MIDIForwardInterface = CmbBoxMIDIForward.Text;
            options.options.MIDIForwardEnabled = ChkBoxMIDIForward.Checked;
            options.options.MIDIFeedbackEnabled = ChkBoxMIDIFeedback.Checked;

            options.options.MidiDeviceStopAllSounds = txtBoxStopAllSoundsDevice.Text;
            options.options.ChannelStopAllSounds = Int32.Parse(txtBoxStopAllSoundsChannel.Text);
            options.options.NoteNumberStopAllSounds = Int32.Parse(txtBoxStopAllSoundsNote.Text);

            CheckedListBox.CheckedItemCollection items = ChkCmbBoxMIDI.CheckedItems;
            options.options.MIDIInterfaces.Clear();
            foreach (object item in items)
            {
                options.options.MIDIInterfaces.Add(item.ToString());
            }
            options.options.Delay = Int32.Parse(txtBoxDelay.Text);

            options.Save();
            this.Close();
        }
    }
}
