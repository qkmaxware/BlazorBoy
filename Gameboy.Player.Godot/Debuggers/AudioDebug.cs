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
    [Export] public Label NR11;
    [Export] public Label Ch1Pace;
    [Export] public Label Ch1SweepDir;
    [Export] public Label NR12;
    [Export] public Label Ch1Duty;
    [Export] public Label Ch1LengthTimer;
    [Export] public Label NR13;
    [Export] public Label Ch1Freq;
    [Export] public Label NR14;
    [Export] public Label Ch1LengthEnable;
    [Export] public Plot2D CH1Plot;

    [ExportGroup("Channel 2")]
    [Export] public Label Ch2On;
    [Export] public Slider Ch2Volume;
    [Export] public Label NR21;
    [Export] public Label Ch2Pace;
    [Export] public Label Ch2SweepDir;
    [Export] public Label NR22;
    [Export] public Label Ch2Duty;
    [Export] public Label Ch2LengthTimer;
    [Export] public Label NR23;
    [Export] public Label Ch2Freq;
    [Export] public Label NR24;
    [Export] public Label Ch2LengthEnable;
    [Export] public Plot2D CH2Plot;

    [ExportGroup("Channel 3")]
    [Export] public Label Ch3On;
    [Export] public Slider Ch3Volume;
    [Export] public Label NR31;
    [Export] public Label NR32;
    [Export] public Label NR33;
    [Export] public Label NR34;

    [ExportGroup("Channel 4")]
    [Export] public Label Ch4On;
    [Export] public Slider Ch4Volume;
    [Export] public Label NR41;
    [Export] public Label NR42;
    [Export] public Label NR43;
    [Export] public Label NR44;

    public void Refresh() {
        var Console = Player?.Console;
        if (Console is null)
            return;

        const int points = 200;
        const float timespan = 1.0f;

        AudioOnButton.ButtonPressed = Speakers?.EnableSound ?? false;

        // CH1
        /*Ch1On.Text          = Console.Sound.Channel1.IsOn().ToString();
        Ch1Volume.Value     = Speakers?.Square1Volume ?? 0.0;

        NR11.Text           = "0b" + Convert.ToString(Console.Sound.Channel1.NRx0, 2);
        Ch1Pace.Text        = "sweep pace:   " + Console.Sound.Channel1.Pace.ToString();
        Ch1SweepDir.Text    = "sweep dir:    " + Console.Sound.Channel1.SweepDirection.ToString();

        NR12.Text           = "0b" + Convert.ToString(Console.Sound.Channel1.NRx1, 2);
        Ch1Duty.Text        = "duty freq:    " + Console.Sound.Channel1.DutyCycle.ToString();
        Ch1LengthTimer.Text = "length timer: " + Console.Sound.Channel1.LengthTimer;

        NR13.Text           = "0b" + Convert.ToString(Console.Sound.Channel1.NRx2, 2);
        Ch1Freq.Text        = "frequency:    " + Console.Sound.Channel1.Frequency.ToString();

        NR14.Text           = "0b" + Convert.ToString(Console.Sound.Channel1.NRx3, 2);
        Ch1LengthEnable.Text= "length enable:" + Console.Sound.Channel1.LengthEnable.ToString();

        var hz = Console.Sound.Channel1.Frequency;
        CH1Plot.Plot(
            Enumerable.Range(0, points)
            .Select(x => new Vector2(
                (x/(float)points) * timespan,
                Math.Sign(Math.Sin(Math.Tau * hz.Hertz * ((x/(float)points) * timespan)))
            )).ToArray()
        );

        // CH2
        Ch2On.Text          = Console.Sound.Channel2.IsOn().ToString();
        Ch2Volume.Value     = Speakers?.Square2Volume ?? 0.0;

        NR21.Text           = "0b" + Convert.ToString(Console.Sound.Channel2.NRx0, 2);
        Ch2Pace.Text        = "sweep pace:   " + Console.Sound.Channel2.Pace.ToString();
        Ch2SweepDir.Text    = "sweep dir:    " + Console.Sound.Channel2.SweepDirection.ToString();

        NR22.Text           = "0b" + Convert.ToString(Console.Sound.Channel2.NRx1, 2);
        Ch2Duty.Text        = "duty freq:    " + Console.Sound.Channel2.DutyCycle.ToString();
        Ch2LengthTimer.Text = "length timer: " + Console.Sound.Channel2.LengthTimer;

        NR23.Text           = "0b" + Convert.ToString(Console.Sound.Channel2.NRx2, 2);
        Ch2Freq.Text        = "frequency:    " + Console.Sound.Channel2.Frequency.ToString();

        NR24.Text           = "0b" + Convert.ToString(Console.Sound.Channel2.NRx3, 2);
        Ch2LengthEnable.Text= "length enable:" + Console.Sound.Channel2.LengthEnable.ToString();

        hz = Console.Sound.Channel2.Frequency;
        CH2Plot.Plot(
            Enumerable.Range(0, points)
            .Select(x => new Vector2(
                (x/(float)points) * timespan,
                Math.Sign(Math.Sin(Math.Tau * hz.Hertz * ((x/(float)points) * timespan)))
            )).ToArray()
        );

        // CH3
        Ch3On.Text          = Console.Sound.Channel3.IsOn().ToString();
        Ch3Volume.Value     = Speakers?.WaveVolume ?? 0.0;

        // CH4
        Ch4On.Text          = Console.Sound.Channel4.IsOn().ToString();
        Ch4Volume.Value     = Speakers?.NoiseVolume ?? 0.0;*/
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