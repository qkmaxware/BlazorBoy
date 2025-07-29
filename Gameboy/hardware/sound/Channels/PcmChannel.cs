using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class PcmChannel : Channel {
    private int[] wave_table = new int[0xFF40 - 0xFF30]; // 16 bytes, each byte holds 2 4-bit
    public Span<int> SampleTable => wave_table;
    public int SampleCount => wave_table.Length; // If not 16 we got a problem
    private int ticksSinceRead = 0;
    private int timer = 0;
    public int WaveTablePosition = 0;

    public int VolumeCode => ((NRX2 >> 5) & 0b0000_0011);
	public float VolumePercent => VolumeCode switch {
		0b00 => 0f,
		0b01 => 0.25f,
		0b10 => 0.5f,
		0b11 => 1.0f,
		_    => 0f
	};

    protected int BaseFrequencyLsb => NRX3;
    protected int BaseFrequencyMsb => NRX4 & 0b0000_0111;
    public int BaseFrequency {
        get => BaseFrequencyLsb | (BaseFrequencyMsb << 8);
        set {
            // Update MSB
            NRX2 = (value & 0b1111_1111);
            // Update LSB
            var four = NRX4;
            four &= 0b1111_1000;                   // Clear last 3 bits of the NRX4 register
            four |= (value >> 8) & 0b0000_0111;    // Set last 3 bits to the top 3 values of the value
            NRX4 = four;
        }
    }
    

    public void ResetWaveTable() {
        // GBC default wave table values
		wave_table[0]  = 0x00;
		wave_table[1]  = 0xFF; 
		wave_table[2]  = 0x00; 
		wave_table[3]  = 0xFF; 
		wave_table[4]  = 0x00; 
		wave_table[5]  = 0xFF; 
		wave_table[6]  = 0x00; 
		wave_table[7]  = 0xFF; 
		wave_table[8]  = 0x00; 
		wave_table[9]  = 0xFF; 
		wave_table[10] = 0x00; 
		wave_table[11] = 0xFF; 
		wave_table[12] = 0x00; 
		wave_table[13] = 0xFF; 
		wave_table[14] = 0x00; 
		wave_table[15] = 0xFF;
    }

    public override void Reset()
    {
        base.Reset();
        ResetWaveTable();
        WaveTablePosition = 0;
        ticksSinceRead = 0;
        timer = 0;
    }

    public override void Tick(ref ClockDelta dt)
    {
        ticksSinceRead++;
        if (timer > 0) {
            timer--;
        }

        if (timer <= 0) {
            timer = (2048 - BaseFrequency) << 1;

            if (IsEnabled) {
                ticksSinceRead = 0;
                WaveTablePosition++;
                if (WaveTablePosition >= wave_table.Length)
                    WaveTablePosition = 0;
            }
        }
    }

    public override bool IsDacEnabled => (NRX0 & 0b1000_0000) != 0;
    public override int LengthLoad {
        get {
            return NRX1 & 0b1111_1111;
        }
        set {
            NRX1 = (value & 0b1111_1111);
        }
    }
    public override int LengthMax => 0b1111_1111;
    public override void OnTrigger() {
        // Wave channel's position is set to 0
        WaveTablePosition = 0;
        ticksSinceRead = 0;
        timer = 0;
    }

    public override void MixInSamples(float playbackFreq, float leftVolume, float rightVolume, Span<Sample> samples)
    {
        if (!IsEnabled)
            return;

        // Calculate frequency in Hz
        int gbFreq = this.BaseFrequency;
        if (gbFreq >= 2048)
            return;
        float hzFreq = 65536.0f / (2048 - gbFreq); // Game Boy PCM channel frequency

        // Phase accumulator for wave table position (0..32)
        float phase = WaveTablePosition;
        float phaseStep = 32.0f * hzFreq / playbackFreq; // 32 samples per waveform cycle

        for (int i = 0; i < samples.Length; i++) {
            int pos = ((int)phase) % 32;
            int waveTableIndex = pos >> 1;
            int sampleByte = wave_table[waveTableIndex];
            int sample;
            if ((pos & 1) != 0) {
                sample = sampleByte & 0x0F;
            } else {
                sample = sampleByte >> 4;
            }

            int dac_input = sample;
            if (VolumeCode > 0) {
                dac_input >>= (VolumeCode - 1);
            } else {
                dac_input = 0;
            }

            float dac_output = (dac_input / 7.5f) - 1.0f;

            var current = samples[i];
            current.Left += leftVolume * dac_output;
            current.Right += rightVolume * dac_output;
            samples[i] = current;

            phase += phaseStep;
            if (phase >= 32.0f)
                phase -= 32.0f;
        }
    }
}