using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class AudioDebug : Control {
    [Export] public GodotBoy Player;
    [Export] public Speakers Speakers;

    [Export] public CheckButton AudioOnButton;

    [ExportGroup("Channel 1")]
    [Export] public Label Ch1On;
    [Export] public Slider Ch1Volume;
    [Export] public AudioDebugChannelDetails Ch1Details;

    [ExportGroup("Channel 2")]
    [Export] public Label Ch2On;
    [Export] public Slider Ch2Volume;
    [Export] public AudioDebugChannelDetails Ch2Details;
    

    [ExportGroup("Channel 3")]
    [Export] public Label Ch3On;
    [Export] public Slider Ch3Volume;
    [Export] public AudioDebugChannelDetails Ch3Details;


    [ExportGroup("Channel 4")]
    [Export] public Label Ch4On;
    [Export] public Slider Ch4Volume;
    [Export] public AudioDebugChannelDetails Ch4Details;

    public void Refresh() {
        var Console = Player?.Console;
        if (Console is null)
            return;

        AudioOnButton.ButtonPressed = Speakers?.EnableSound ?? false;

        // CH1
        Ch1On.Text          = Console.Sound.Channel1.IsEnabled.ToString();
        Ch1Volume.Value     = Speakers?.Square1Volume ?? 0.0;
        Ch1Details?.Refresh(Console.Sound.Channel1);

        // CH2
        Ch2On.Text          = Console.Sound.Channel2.IsEnabled.ToString();
        Ch2Volume.Value     = Speakers?.Square2Volume ?? 0.0;
        Ch2Details?.Refresh(Console.Sound.Channel2);

        // CH3
        Ch3On.Text          = Console.Sound.Channel3.IsEnabled.ToString();
        Ch3Volume.Value     = Speakers?.WaveVolume ?? 0.0;
        Ch3Details?.Refresh(Console.Sound.Channel3);

        // CH4
        Ch4On.Text          = Console.Sound.Channel4.IsEnabled.ToString();
        Ch4Volume.Value     = Speakers?.NoiseVolume ?? 0.0;
        Ch4Details?.Refresh(Console.Sound.Channel4);
    }

    public void OnSliderChanged(bool changed) {
        if (changed) {
            PushbackSettings();
        }
    }
    public void PushbackSettings() {
        if (Player is not null)
            PushbackConsoleSettings(Player.Console);
        if (Speakers is not null) 
            PushbackSpeakerSettings(Speakers);
    }
    private void PushbackConsoleSettings(Gameboy gameboy) {

    }

    private void PushbackSpeakerSettings(Speakers speakers) {
        if (speakers is null)
            return;

        speakers.EnableSound = AudioOnButton.ButtonPressed;

        speakers.Square1Volume = (float)Ch1Volume.Value;
        speakers.Square2Volume = (float)Ch2Volume.Value;
        speakers.WaveVolume    = (float)Ch3Volume.Value;
        speakers.NoiseVolume   = (float)Ch4Volume.Value;
    }
}