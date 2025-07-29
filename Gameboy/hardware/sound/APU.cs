using System.IO.Compression;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class APU : IMemorySegment {
    private FrameSequencer sequencer;
    public SquareWaveChannel Channel1 = new SquareWaveChannel();
	public SquareWaveChannel Channel2 = new SquareWaveChannel(use_sweep: false);
	public PcmChannel Channel3 = new PcmChannel();
	public NoiseChannel Channel4 = new NoiseChannel();
	
	public SquareWaveChannel Square1 => Channel1;
	public SquareWaveChannel Square2 => Channel2;
	public PcmChannel Wave => Channel3;
	public NoiseChannel Noise => Channel4;

    private int FF24_MasterVolume_VINPanning;
    public int LeftVolume => (FF24_MasterVolume_VINPanning & 0b0111_0000) >> 4;
    public int RightVolume => (FF24_MasterVolume_VINPanning & 0b0000_0111);

    private int FF25_SoundPanning;
    private int FF26_AudioMasterControl;
    public bool IsPoweredOn => (FF26_AudioMasterControl  & 0b1000_0000) != 0;

    public APU() {
        sequencer = new FrameSequencer(Channel1, Channel2, Channel3, Channel4);
    }

    public void Tick(ClockDelta dt) {
		// Called every instruction
        Channel1.Tick(ref dt);
        Channel2.Tick(ref dt);
        Channel3.Tick(ref dt);
        Channel4.Tick(ref dt);
        sequencer.Step(ref dt);
    }

    private MemoryMap? mmu;
	public void SetMMU(MemoryMap mmu) {
        this.mmu = mmu;
    }

    public void Reset() {
		Channel1.Reset();
		Channel2.Reset();
		Channel3.Reset();
		Channel4.Reset();
		
		FF24_MasterVolume_VINPanning = 0;
		FF25_SoundPanning = 0;
		FF26_AudioMasterControl = 0;
	}

    public int ReadByte(int addr) {
		if (addr < 0xFF10 || addr > 0xFF3F) {
			return 0;
		}
		
		switch (addr) {
			// Channels
			case 0xFF10: return Channel1.NRX0;
			case 0xFF11: return Channel1.NRX1;
			case 0xFF12: return Channel1.NRX2;
			case 0xFF13: return Channel1.NRX3;
			case 0xFF14: return Channel1.NRX4;
			
			case 0xFF15: return Channel2.NRX0;
			case 0xFF16: return Channel2.NRX1;
			case 0xFF17: return Channel2.NRX2;
			case 0xFF18: return Channel2.NRX3;
			case 0xFF19: return Channel2.NRX4;
			
			case 0xFF1A: return Channel3.NRX0;
			case 0xFF1B: return Channel3.NRX1;
			case 0xFF1C: return Channel3.NRX2;
			case 0xFF1D: return Channel3.NRX3;
			case 0xFF1E: return Channel3.NRX4;
			
			case 0xFF1F: return Channel4.NRX0;
			case 0xFF20: return Channel4.NRX1;
			case 0xFF21: return Channel4.NRX2;
			case 0xFF22: return Channel4.NRX3;
			case 0xFF23: return Channel4.NRX4;
			
			// Control & Status
			case 0xFF24: return FF24_MasterVolume_VINPanning;
			case 0xFF25: return FF25_SoundPanning;
			case 0xFF26: return FF26_AudioMasterControl;
			
			// Unused
			case >= 0xFF27 and 0xFF2F:
				return 0xFF;
			
			// Wave Table
			case >= 0xFF30 and <= 0xFF3F:
				return this.Channel3.SampleTable[addr - 0xFF30];
			
			default: return 0;
		}
	}
	
	public void WriteByte(int addr, int value) {
		if (addr < 0xFF10 || addr > 0xFF3F) {
			return;
		}
		
		var power = this.IsPoweredOn;
		
		switch (addr) {
			// Channels
			case 0xFF10: Channel1.NRX0 = value; break;
			case 0xFF11: Channel1.NRX1 = value; break;
			case 0xFF12: Channel1.NRX2 = value; break;
			case 0xFF13: Channel1.NRX3 = value; break;
			case 0xFF14: Channel1.NRX4 = value; break;
									  
			case 0xFF15: Channel2.NRX0 = value; break;
			case 0xFF16: Channel2.NRX1 = value; break;
			case 0xFF17: Channel2.NRX2 = value; break;
			case 0xFF18: Channel2.NRX3 = value; break;
			case 0xFF19: Channel2.NRX4 = value; break;
									   
			case 0xFF1A: Channel3.NRX0 = value; break;
			case 0xFF1B: Channel3.NRX1 = value; break;
			case 0xFF1C: Channel3.NRX2 = value; break;
			case 0xFF1D: Channel3.NRX3 = value; break;
			case 0xFF1E: Channel3.NRX4 = value; break;
									   
			case 0xFF1F: Channel4.NRX0 = value; break;
			case 0xFF20: Channel4.NRX1 = value; break;
			case 0xFF21: Channel4.NRX2 = value; break;
			case 0xFF22: Channel4.NRX3 = value; break;
			case 0xFF23: Channel4.NRX4 = value; break;
			
			// Control & Status
			case 0xFF24: FF24_MasterVolume_VINPanning = value; break;
			case 0xFF25: FF25_SoundPanning = value; break;
			case 0xFF26: FF26_AudioMasterControl = value; break;
			
			// Wave Table
			case >= 0xFF30 and <= 0xFF3F: 
				this.Channel3.SampleTable[addr - 0xFF30] = value;
				break;
		}
		
		// If power status has changed, power on or off
		if (power != this.IsPoweredOn) {
			switch (this.IsPoweredOn) {
				case true: onPoweredOn(); break;
				case false: onPoweredOff(); break;
			}
		}
	}

    private void onPoweredOn() {
		// GBC default wave table values
        Channel3.ResetWaveTable();
	}
	
	private void onPoweredOff() {
		Channel1.Reset();
		Channel2.Reset();
		Channel3.Reset();
		Channel4.Reset();
	}

	public void FillSamples(float playbackFreq, Span<Sample> samples, float chan1Volume = 1.0f, float chan2Volume = 1.0f, float chan3Volume = 1.0f, float chan4Volume = 1.0f) {
        // Clear samples
		samples.Fill(new Sample());

		if (!IsPoweredOn)
            return; // Done, no audio is generated
	
		// Mix in samples
		var lv = LeftVolume / 7.0f;
		var rv = RightVolume / 7.0f;
        Channel1.MixInSamples(playbackFreq, chan1Volume * lv, chan1Volume * rv, samples);
        Channel2.MixInSamples(playbackFreq, chan2Volume * lv, chan2Volume * rv, samples);
        Channel3.MixInSamples(playbackFreq, chan3Volume * lv, chan3Volume * rv, samples);
        Channel4.MixInSamples(playbackFreq, chan4Volume * lv, chan4Volume * rv, samples);

		// Average samples across channels
		for (var i = 0; i < samples.Length; i++) {
			var sample = samples[i];
			sample.Left /= 4;
			sample.Right /= 4;
			samples[i] = sample;
		}
    }
}