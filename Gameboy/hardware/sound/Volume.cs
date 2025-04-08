namespace Qkmaxware.Emulators.Gameboy.Hardware;

public struct Volume {
	private float percent;

	public Volume(int value) {
		if (value < 0)
			value = 0;
		if (value > 7)
			value = 7;
		percent = value / 7.0f;
	}
    public Volume (float value) {
        if (value < 0)
            value = 0;
        if (value > 1)
            value = 1;
        percent = value;
    }

	public float Percent => percent * 100;
	public float NormalizedPercent => percent;

	public override string ToString() => $"{NormalizedPercent}%";
}

public enum VolumeCode {
	Volume0 = 0b00,
	Volume100 = 0b01,
	Volume50 = 0b10,
	Volume25 = 0b11
}