using Godot;
using Qkmaxware.Emulators.Gameboy.Hardware;
using System;

public partial class AudioDebugChannelDetails : MarginContainer
{
    [ExportGroup("Registers")]
    [Export] public Label NRx0;
    [Export] public Label NRx1;
    [Export] public Label NRx2;
    [Export] public Label NRx3;
    [Export] public Label NRx4;

    public void Refresh(Channel audioChannel) {
        NRx0.Text = audioChannel.NRX0.ToString("X2");
        NRx1.Text = audioChannel.NRX1.ToString("X2");
        NRx2.Text = audioChannel.NRX2.ToString("X2");
        NRx3.Text = audioChannel.NRX3.ToString("X2");
        NRx4.Text = audioChannel.NRX4.ToString("X2");


    }
}
