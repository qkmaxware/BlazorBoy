using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class SquareWaveChannel : Channel {

    public SquareWaveChannel(bool use_sweep = false) {
        this.UseSweep = use_sweep;
    }

    public int SweepPeriod {
        get => (NRX0 & 0b0111_0000) >> 4;
        set {
            NRX0 = (NRX0 & 0b1000_1111) | ((value & 0b111) << 4);
        }
    }

    public Direction SweepNegate {
        get => (Direction)((NRX0 & 0b0000_1000) >> 3);
        set {
            var cleared = (NRX0 & 0b1111_0111);
            var set = (((int)value & 0b1) << 3);
            NRX0 = cleared | set;
        }
    }

    public int SweepShift {
        get => NRX0 & 0b000_0111;
        set {
            NRX0 = (NRX0 & 0b1111_1000) | (value & 0b111);
        }
    }
    public bool UseSweep { get; private set; }
    public bool SweepEnabled { get; set; }
    public int CurrentFrequency {get; set;}
    public int SweepTimer {get; set;}

    public int StartingVolume {
        get => (NRX2 & 0b1111_0000) >> 4;
        set {
            NRX2 = (NRX2 & 0b0000_1111) | ((value & 0b1111) << 4);
        }
    }
    public int CurrentVolume { get; set; }
    public int MaxVolume => 0xF;
    public int MinVolume => 0x0;
    public float CurrentVolumePercent => (float)CurrentVolume / (float)MaxVolume;

    public Direction EnvelopeMode {
        get => (Direction)((NRX2 & 0b0000_1000) >> 3);
        set {
            var cleared = (NRX2 & 0b1111_0111);
            var set = (((int)value & 0b1) << 3);
            NRX2 = cleared | set;
        }
    }

    public int EnvelopePeriod {
        get => (NRX2 & 0b0000_0111) >> 0;
        set {
            var cleared = (NRX2 & 0b1111_1000);
            var set = value & 0b111;
            NRX2 = cleared | set;
        }
    }
    public int PeriodTimer {get; set;}

    public Hardware.Duty Duty {
        get => (Hardware.Duty)((NRX1 & 0b1100_0000) >> 6);
        set => NRX1 = 
            (NRX1 & 0b0011_1111)
            | ((((int)value) & 0b0000_0011) << 6);
    }

    protected int BaseFrequencyLsb => NRX3;
    protected int BaseFrequencyMsb => NRX4 & 0b0000_0111;
    public int BaseFrequency {
        get => BaseFrequencyLsb | (BaseFrequencyMsb << 8);
        set {
            // Update MSB
            NRX3 = (value & 0b1111_1111);
            // Update LSB
            var four = NRX4;
            four &= 0b1111_1000;                   // Clear last 3 bits of the NRX4 register
            four |= (value >> 8) & 0b0000_0111;    // Set last 3 bits to the top 3 values of the value
            NRX4 = four;
        }
    }
    public int FrequencyTimer;

    public SquareWaveWaveform Waveform => this.Duty switch {
        Hardware.Duty.TwelvePointFive => new SquareWaveWaveform(0b0000_0001),
        Hardware.Duty.TwentyFive => new SquareWaveWaveform(0b1000_0001),
        Hardware.Duty.Fifty => new SquareWaveWaveform(0b1000_0111),
        Hardware.Duty.SeventyFive => new SquareWaveWaveform(0b0111_1110),
        _ => new SquareWaveWaveform(0b0000_0000),
    };
    public int WaveDutyRegisterPosition;

    public override void Tick(ref ClockDelta dt) {
        if (dt.M <= 0)
            return;

        if (this.FrequencyTimer > 0) {
            this.FrequencyTimer--;
            if (this.FrequencyTimer == 0) {
                this.FrequencyTimer = (2048 - this.BaseFrequency) * 4;
                WaveDutyRegisterPosition = (WaveDutyRegisterPosition + 1) % 8;
            }
        }
    }

    public override void MixInSamples(float playbackFreq, float leftVolume, float rightVolume, Sample[] samples) {
        if (!IsEnabled)
            return;

        // Calculate actual freq in Hz currently
        int gbFreq = this.BaseFrequency;
        if (gbFreq >= 2048)
            return;
        float hzFreq = 131072.0f / (2048 - gbFreq);

        // Phase accumulator for waveform position
        float phase = WaveDutyRegisterPosition;
        float phaseStep = 8.0f * hzFreq / playbackFreq; // 8 steps per waveform cycle
        var waveform = this.Waveform;

        for (int i = 0; i < samples.Length; i++) {
            int pos = (int)phase % 8;

            var dac_input = (waveform.IsHigh(pos) ? 1.0f : 0.0f) * CurrentVolume;
            var dac_output = (dac_input / 7.5f) - 1.0f;

            var current = samples[i];
            current.Left += leftVolume * dac_output;
            current.Right += rightVolume * dac_output;
            samples[i] = current;

            phase += phaseStep;
            if (phase >= 8.0f)
                phase -= 8.0f;
        }
    }

    public override void Reset() {
        // Reset underlying hardware registers
        base.Reset();

        // Reset channel-specific "hidden" registers
        this.CurrentVolume = 0;
        this.PeriodTimer = 0;
        this.FrequencyTimer = 0;
        this.WaveDutyRegisterPosition = 0;
    }

    public override void OnTrigger() {
        // Frequency timer is loaded with period
        this.CurrentFrequency = this.BaseFrequency;
        this.SweepTimer = this.SweepPeriod != 0 ? this.SweepPeriod : 8;
        this.SweepEnabled = this.SweepPeriod != 0 || this.SweepShift != 0;
        if (this.SweepShift != 0 && this.CurrentFrequency > 2047) {
            this.IsEnabled = false;
        }

        // Volume envelope timer is loaded with period
        this.PeriodTimer = this.EnvelopePeriod;

        // Channel volume is loaded with NRx2
        this.CurrentVolume = this.StartingVolume;

        // Frequency timer
        this.FrequencyTimer = (2048 - this.BaseFrequency) * 4;
        this.WaveDutyRegisterPosition = 0;
    }

    public override void ClockSweep(int fsStep) { 
        // Only for channel 1 (channel 2 has no sweep)
        if (!UseSweep)
            return;

        bool wasNegated = SweepTimer > 0;
        if (wasNegated) {
            SweepTimer -= 1;
        }

        if (SweepTimer != 0) {
            return;
        }

        this.SweepTimer = SweepPeriod > 0 ? SweepPeriod : 8;

        if (SweepEnabled && (SweepPeriod > 0)) {
            var new_freq = calculateFrequency(out var shouldDisable);
            IsEnabled = !shouldDisable;

            if (new_freq <= 2047 && SweepShift > 0) {
                this.BaseFrequency = new_freq;
                this.CurrentFrequency = new_freq;

                calculateFrequency(out shouldDisable);
                IsEnabled = !shouldDisable;
            }
        }
    }

    private int calculateFrequency(out bool disable_channel) {
        var freq = CurrentFrequency >> SweepShift;

        if (SweepNegate == Direction.Increasing) {
            freq = CurrentFrequency + freq;
        } else {
            freq = CurrentFrequency - freq;
        }

        // Overflow check
        disable_channel = freq > 2047;
        return freq;
    }

    public override void ClockVolumeEnvelope(int fsStep) { 
        var initialVolume = this.StartingVolume;
        var currentVolume = this.CurrentVolume;
        var mode = this.EnvelopeMode;
        var period = this.EnvelopePeriod;
        var timer = this.PeriodTimer;

        if (period == 0)
            return;
        
        var didDecrement = timer > 0;
        if (didDecrement) {
            timer -= 1;
        }
        this.PeriodTimer = timer;

        if (timer != 0)
            return;

        timer = period;
        this.PeriodTimer = timer;

        if (currentVolume < 0xF && mode == Direction.Increasing) {
            currentVolume++;
            this.CurrentVolume = currentVolume;
        } else if (currentVolume > 0 && mode == Direction.Decreasing) {
            currentVolume--;
            this.CurrentVolume = currentVolume;
        }
    }
}