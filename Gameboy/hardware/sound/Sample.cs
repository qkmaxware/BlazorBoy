namespace Qkmaxware.Emulators.Gameboy.Hardware;

public struct Sample {
    public float Left;
    public float Right;

    public Sample() {
        this.Left = 0; this.Right = 0;
    }
    public Sample(float left, float right) {
        this.Left = left; this.Right = right;
    }
}