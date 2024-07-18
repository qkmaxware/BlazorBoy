// Sound hardware created from the description on the site below
// https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware
// Might be able to be emulated in Godot using https://docs.godotengine.org/en/stable/classes/class_audiostreamgenerator.html
namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Channel {
	public int NRx0;
	public int NRx1;
	public int NRx2;
	public int NRx3;
	public int NRx4;
	
	public void Reset() {
		NRx0 = NRx1 = NRx2 = NRx3 = NRx4 = 0;
	}
}

public class SquareWaveChannel : Channel {
	public int SweepPeriod => (NRx0 >> 4) & (0b0000_0111);
	public bool Negate => ((NRx0 >> 3) & (0b0000_0001)) != 0;
	public int Shift => (NRx0 >> 0) & (0b0000_0111);
	
	public int Duty => (NRx1 >> 6) & (0b0000_0011);
	public int LengthLoad => (NRx1 >> 0) & (0b0011_1111);
	
	public int StartingVolume => (NRx2 >> 4) & (0b0000_1111);
	public bool EnvelopeAddMode => ((NRx2 >> 3) & (0b0000_0001)) != 0;
	public int Period => (NRx2 >> 0) & (0b0000_0111);
	
	public int FrequencyLsb => NRx3;
	
	public bool Trigger => ((NRx4 >> 7) & 0b0000_0001) != 0;
	public bool LengthEnable => ((NRx4 >> 6) & 0b0000_0001) != 0;
	public int FrequencyMsb => NRx4 & 0b0000_0111;
}

public enum VolumeCode {
	Volume0 = 0b00,
	Volume100 = 0b01,
	Volume50 = 0b10,
	Volume25 = 0b11
}

public class WaveChannel : Channel {
	public bool DacPower => ((NRx0) & (0b1000_0000)) != 0;
	public int LengthLoad => NRx1;
	public VolumeCode Volume => ((NRx2 >> 5) & 0b0000_0011) switch {
		0b00 => VolumeCode.Volume0,
		0b01 => VolumeCode.Volume100,
		0b10 => VolumeCode.Volume50,
		0b11 => VolumeCode.Volume25,
		_ 	 => VolumeCode.Volume0
	};
	public int FrequencyLsb => NRx3;
	public bool Trigger => ((NRx4 >> 7) & 0b0000_0001) != 0;
	public bool LengthEnable => ((NRx4 >> 6) & 0b0000_0001) != 0;
	public int FrequencyMsb => NRx4 & 0b0000_0111;
}

public class NoiseChannel : Channel {
	public int LengthLoad => ((NRx1 >> 0) & 0b0011_1111);
	public int StartingVolume => (NRx2 >> 4) & (0b0000_1111);
	public bool EnvelopeAddMode => ((NRx2 >> 3) & (0b0000_0001)) != 0;
	public int Period => (NRx2 >> 0) & (0b0000_0111);
	public int ClockShift => (NRx3 >> 4) & 0b0000_1111;
	public bool WidthModeOfLfsr => ((NRx3 >> 0) & 0b0000_1000) != 0;
	public int DivisorCode => (NRx3 >> 0) & 0b0000_0111;
	public bool Trigger => ((NRx4 >> 7) & 0b0000_0001) != 0;
	public bool LengthEnable => ((NRx4 >> 6) & 0b0000_0001) != 0;
}

public class Sound : IMemorySegment {

	public SquareWaveChannel Channel1 = new SquareWaveChannel();
	public SquareWaveChannel Channel2 = new SquareWaveChannel();
	public WaveChannel Channel3 = new WaveChannel();
	public NoiseChannel Channel4 = new NoiseChannel();
	
	public SquareWaveChannel Square1 => Channel1;
	public SquareWaveChannel Square2 => Channel2;
	public WaveChannel Wave => Channel3;
	public NoiseChannel Noise => Channel4;

	private int FF24;
	public bool VinLeftEnabled => (FF24 & 0b1000_0000) != 0;
	public bool VinRightEnabled => (FF24 & 0b0000_1000) != 0;
	public int LeftVolume => ((FF24 >> 4) & 0b0000_0111);
	public int RightVolume => ((FF24 >> 0) & 0b0000_0111);
	
	private int FF25;
	public bool IsLeftOn => (FF25  & 0b1111_0000) != 0;
	public bool IsRightOn => (FF25  & 0b0000_1111) != 0;
	
	private int FF26;
	public bool IsPoweredOn => (FF26  & 0b1000_0000) != 0;
	public int ChannelLengthStatuses => (FF26  & 0b0000_1111);
	
	private int[] WaveTable = new int[0xFF40 - 0xFF30];

	public void Reset() {
		Channel1.Reset();
		Channel2.Reset();
		Channel3.Reset();
		Channel4.Reset();
		
		FF24 = 0;
		FF25 = 0;
		FF26 = 0;
		
		Array.Fill(WaveTable, 0);
	}
	
	private MemoryMap? mmu;
	public void SetMMU(MemoryMap mmu) {
        this.mmu = mmu;
    }

	public int ReadByte(int addr) {
		if (addr < 0xFF10 || addr > 0xFF3F) {
			return 0;
		}
		
		switch (addr) {
			// Channels
			case 0xFF10: return Channel1.NRx0;
			case 0xFF11: return Channel1.NRx1;
			case 0xFF12: return Channel1.NRx2;
			case 0xFF13: return Channel1.NRx3;
			case 0xFF14: return Channel1.NRx4;
			
			case 0xFF15: return Channel2.NRx0;
			case 0xFF16: return Channel2.NRx1;
			case 0xFF17: return Channel2.NRx2;
			case 0xFF18: return Channel2.NRx3;
			case 0xFF19: return Channel2.NRx4;
			
			case 0xFF1A: return Channel3.NRx0;
			case 0xFF1B: return Channel3.NRx1;
			case 0xFF1C: return Channel3.NRx2;
			case 0xFF1D: return Channel3.NRx3;
			case 0xFF1E: return Channel3.NRx4;
			
			case 0xFF1F: return Channel4.NRx0;
			case 0xFF20: return Channel4.NRx1;
			case 0xFF21: return Channel4.NRx2;
			case 0xFF22: return Channel4.NRx3;
			case 0xFF23: return Channel4.NRx4;
			
			// Control & Status
			case 0xFF24: return FF24;
			case 0xFF25: return FF25;
			case 0xFF26: return FF26;
			
			// Unused
			case >= 0xFF27 and 0xFF2F:
				return 0xFF;
			
			// Wave Table
			case >= 0xFF30 and <= 0xFF3F:
				return WaveTable[addr - 0xFF30];
			
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
			case 0xFF10: Channel1.NRx0 = value; break;
			case 0xFF11: Channel1.NRx1 = value; break;
			case 0xFF12: Channel1.NRx2 = value; break;
			case 0xFF13: Channel1.NRx3 = value; break;
			case 0xFF14: Channel1.NRx4 = value; break;
									  
			case 0xFF15: Channel2.NRx0 = value; break;
			case 0xFF16: Channel2.NRx1 = value; break;
			case 0xFF17: Channel2.NRx2 = value; break;
			case 0xFF18: Channel2.NRx3 = value; break;
			case 0xFF19: Channel2.NRx4 = value; break;
									   
			case 0xFF1A: Channel3.NRx0 = value; break;
			case 0xFF1B: Channel3.NRx1 = value; break;
			case 0xFF1C: Channel3.NRx2 = value; break;
			case 0xFF1D: Channel3.NRx3 = value; break;
			case 0xFF1E: Channel3.NRx4 = value; break;
									   
			case 0xFF1F: Channel4.NRx0 = value; break;
			case 0xFF20: Channel4.NRx1 = value; break;
			case 0xFF21: Channel4.NRx2 = value; break;
			case 0xFF22: Channel4.NRx3 = value; break;
			case 0xFF23: Channel4.NRx4 = value; break;
			
			// Control & Status
			case 0xFF24: FF24 = value; break;
			case 0xFF25: FF25 = value; break;
			case 0xFF26: FF26 = value; break;
			
			// Wave Table
			case >= 0xFF30 and <= 0xFF3F: 
				WaveTable[addr - 0xFF30] = value;
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
		WaveTable[0]  = 0x00;
		WaveTable[1]  = 0xFF; 
		WaveTable[2]  = 0x00; 
		WaveTable[3]  = 0xFF; 
		WaveTable[4]  = 0x00; 
		WaveTable[5]  = 0xFF; 
		WaveTable[6]  = 0x00; 
		WaveTable[7]  = 0xFF; 
		WaveTable[8]  = 0x00; 
		WaveTable[9]  = 0xFF; 
		WaveTable[10] = 0x00; 
		WaveTable[11] = 0xFF; 
		WaveTable[12] = 0x00; 
		WaveTable[13] = 0xFF; 
		WaveTable[14] = 0x00; 
		WaveTable[15] = 0xFF;
	}
	
	private void onPoweredOff() {
		Channel1.Reset();
		Channel2.Reset();
		Channel3.Reset();
		Channel4.Reset();
	}

}
