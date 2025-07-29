using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class NoiseChannel : Channel
{

    private int lfsr = 0x7FFF;
    private int frequencyTimer = 0;
    private int envelopeVolume = 0;
    private int envelopeTimer = 0;

    public int Divisor => NRX3 & 0b111 switch {
        0 => 8,
        _ => (NRX3 & 0b111) << 4
    };

    public int ShiftClockFrequency => (NRX3 >> 4) & 0b1111;

    public bool WidthMode => (NRX3 & 0b1000) != 0;

    public override void Reset() {
        base.Reset();
        this.frequencyTimer = 0;
        this.envelopeVolume = (NRX2 >> 4) & 0xF; // Set to initial envelope volume
        this.envelopeTimer = (NRX2 & 0b111);     // Set to envelope period
    }

    public override void OnTrigger() {
        this.envelopeVolume = (NRX2 >> 4) & 0xF;
        this.envelopeTimer = (NRX2 & 0b111);
    }

    public override void MixInSamples(float playbackFreq, float leftVolume, float rightVolume, Span<Sample> samples)
    {
        if (!IsEnabled)
            return;
        
        // Calculate the noise channel's output frequency
        int divisor = Divisor;
        int shift = ShiftClockFrequency;
        float freq = 524288.0f / divisor / (1 << (shift + 1)); // Game Boy noise channel frequency formula
        
        // For each sample, step the LFSR and mix output
        for (int i = 0; i < samples.Length; i++)
        {
            // Step the frequency timer
            frequencyTimer--;
            if (frequencyTimer <= 0)
            {
                frequencyTimer = (int)(playbackFreq / freq);
                // LFSR feedback calculation
                int bit = ((lfsr & 1) ^ ((lfsr >> 1) & 1));
                lfsr = (ushort)((lfsr >> 1) | (bit << 14));
                if (WidthMode)
                {
                    // 7-bit mode: also set bit 6
                    lfsr = (ushort)((lfsr & ~(1 << 6)) | (bit << 6));
                }
            }

            // Output is the inverse of the LFSR's lowest bit
            float sampleValue = ((lfsr & 1) == 0) ? envelopeVolume / 15.0f : 0.0f;

            // Mix into left/right channels
            samples[i].Left += sampleValue * leftVolume;
            samples[i].Right += sampleValue * rightVolume;
        }
    }

    public override void Tick(ref ClockDelta dt)
    {
        
    }

    public override void ClockVolumeEnvelope(int fsStep)
    {
        int period = NRX2 & 0b111;
        if (period == 0) period = 8; // Hardware treats 0 as 8

        if (envelopeTimer > 0)
            envelopeTimer--;
        if (envelopeTimer == 0)
        {
            envelopeTimer = period;
            int direction = (NRX2 & 0b1000) != 0 ? 1 : -1;
            if ((direction > 0 && envelopeVolume < 15) || (direction < 0 && envelopeVolume > 0))
            {
                envelopeVolume += direction;
            }
        }
    }
}