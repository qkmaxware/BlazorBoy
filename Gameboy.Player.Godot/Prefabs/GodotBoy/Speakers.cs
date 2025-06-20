using Godot;
using Qkmaxware.Emulators.Gameboy;
using Qkmaxware.Emulators.Gameboy.Hardware;
using System;
using System.IO;
using System.Numerics;

public partial class Speakers : Control {

	[Export] public bool EnableSound = false;

    [ExportGroup("Sound Mixer")]
    [Export(PropertyHint.Range, "0,1")] public float MasterVolume = 1.0f;
    [Export(PropertyHint.Range, "0,1")] public float Square1Volume = 1.0f;
    [Export(PropertyHint.Range, "0,1")] public float Square2Volume = 1.0f;
    [Export(PropertyHint.Range, "0,1")] public float WaveVolume = 1.0f;
    [Export(PropertyHint.Range, "0,1")] public float NoiseVolume = 0.2f;


	// Audio stream generator for playback
    private AudioStreamPlayer player;
    private AudioStreamGenerator generator;
    private AudioStreamGeneratorPlayback playback;

	// Sample rate (GameBoy typically runs at 44100 Hz)
    private const int SampleRate = 44100; // 44100 cycles per second

	// Audio channel data
    private Godot.Vector2[] audioBuffer;
    private Sample[] sampleBuffer;

	public override void _Ready() {
        var bufferSize = (SampleRate / Godot.Engine.MaxFps) + 1; // 44100 / 60 
        audioBuffer = new Godot.Vector2[bufferSize];
        sampleBuffer = new Sample[bufferSize];
        SetAudio(EnableSound);

        // Initialize the AudioStreamGenerator
        player = GetNode<AudioStreamPlayer>("Player");
        if (player.Stream is not AudioStreamGenerator stream) {
            return;
        }
        generator = stream;
        generator.MixRate = SampleRate;
        player.Play();
        
        playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    public void ToggleAudio() {
        SetAudio(!EnableSound);
    }
    public void SetAudio(bool enable) {
        EnableSound = enable;
    }

    public void GenerateAudio(Gameboy gameboy, double dt) {
        // Clear the buffer
        Array.Fill(audioBuffer, Godot.Vector2.Zero);

        // Generate the audio
        if (EnableSound && gameboy.Sound.IsPoweredOn) {
            GenerateAudio(gameboy.Sound);
        }

        // Fill the buffer for audio playback
        playback.PushBuffer(audioBuffer);
        return;
    }

	private void GenerateAudio(APU apu) {
        // Mix the sounds of all channels
        apu.FillSamples(
            SampleRate,  // 44100Hz
            sampleBuffer, 
            MasterVolume * Square1Volume,
            MasterVolume * Square2Volume,
            MasterVolume * WaveVolume,
            MasterVolume * NoiseVolume
        );
        for (var i = 0; i < sampleBuffer.Length; i++) {
            var sample = sampleBuffer[i];
            audioBuffer[i] = new Godot.Vector2(sample.Left, sample.Right);
        } 
    }
}
