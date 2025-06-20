namespace Qkmaxware.Emulators.Gameboy.Hardware;

public enum Duty {
    TwelvePointFive = 0,
    TwentyFive = 1,
    Fifty = 2,
    SeventyFive = 3
}

public struct SquareWaveWaveform {
    private int pattern;

    public SquareWaveWaveform(int pattern) {
        this.pattern = pattern & 0b1111_1111;
    }

    public bool IsHigh(int fsStep) {
        fsStep = fsStep % 8;
        return (pattern & (0b1000_000 >> fsStep)) != 0;
    }

    public bool IsLow(int fsStep) => !IsHigh(fsStep);
}