namespace Qkmaxware.Emulators.Gameboy.Hardware;

public enum ChannelStatus {
	Unknown,
	On,
	Off
}

public abstract class Channel {
	public int NRx0;
	public int NRx1;
	public int NRx2;
	public int NRx3;
	public int NRx4;
	
	public void Trigger() {
		NRx4 = NRx4 | 0b1000_0000;
	}

	public bool IsTriggered => ((NRx4 >> 7) & 0b1000_0000) != 0;

	public void Reset() {
		NRx0 = NRx1 = NRx2 = NRx3 = NRx4 = 0;
	}

	public virtual ChannelStatus IsOn() => ChannelStatus.On;
	public abstract void Tick();
}

public class SquareWaveChannel : Channel {
	public override ChannelStatus IsOn() {
		return (this.LengthEnable && this.LengthTimer > 0) switch {
			true 	=> ChannelStatus.On,
			_ 		=> ChannelStatus.Off
		};
	}

	public int SweepControls => NRx0;
    public Frequency Pace => new Frequency(((SweepControls & 0b0111_0000) >> 4) * 128);
    public Direction Direction => (SweepControls & 0b1000_0000) == 0 ? Direction.Increasing : Direction.Decreasing;
	
    public int LengthTimeAndDutyCycle => NRx1;
    public Duty Duty => new Duty(
        ((LengthTimeAndDutyCycle >> 6) & (0b0000_0011)) switch {
            0b00 => 0.125f,
            0b01 => 0.25f,
            0b10 => 0.5f,
            0b11 => 0.75f,
			_ => 0f
        }
    );
	public int LengthTimer => (NRx1 >> 0) & (0b0011_1111);
	
    public int VolumeAndEnvelope => NRx2;
	public Volume StartingVolume => new Volume(((VolumeAndEnvelope >> 4) & (0b0000_1111)) / 16.0f);
	public Direction EnvelopeDirection => (((VolumeAndEnvelope >> 3) & (0b0000_0001)) != 0) ? Direction.Increasing : Direction.Decreasing;
	public Frequency SweepPace => new Frequency( ((VolumeAndEnvelope >> 0) & (0b0000_0111)) * 64 );
	public bool IsEnvelopeEnabled => ((VolumeAndEnvelope >> 0) & (0b0000_0111)) != 0;
	
	public int FrequencyLsb => NRx3;

	public bool LengthEnable => ((NRx4 >> 6) & 0b0000_0001) != 0;
	public int FrequencyMsb => NRx4 & 0b0000_0111;

	public Frequency Frequency => new Frequency(
		131072 / (2048 - ((FrequencyMsb << 8) | FrequencyLsb))
	);

	public override void Tick() {}
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
	public bool WaveTrigger => ((NRx4 >> 7) & 0b0000_0001) != 0;
	public bool LengthEnable => ((NRx4 >> 6) & 0b0000_0001) != 0;
	public int FrequencyMsb => NRx4 & 0b0000_0111;

	public override void Tick() {}
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

	public override void Tick() {}
}