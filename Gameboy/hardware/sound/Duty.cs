namespace Qkmaxware.Emulators.Gameboy.Hardware;

public struct Duty {
	private float percent;

    public Duty (float value) {
        if (value < 0)
            value = 0;
        if (value > 1)
            value = 1;
        percent = value;
    }

	public float Percent => percent * 100;
	public float NormalizedPercent => percent;

	public override string ToString() => $"{Percent}% of period";
}