namespace Qkmaxware.Emulators.Gameboy.Hardware;

public struct Frequency {
	private float val;

	public Frequency(float value) {
		this.val = value;
	}

    public float Hertz => val;

	public Period AsPeriod() => new Period(1 / this.Hertz);

	public override string ToString() => $"{Hertz}Hz";
}

public struct Period {
	private float seconds;

	public Period(float seconds) {
		this.seconds = seconds;
	}

	public float Seconds => this.seconds;

	public Frequency AsFrequency() => new Frequency(1 / this.Seconds);

	public override string ToString() => $"{Seconds}s";
}