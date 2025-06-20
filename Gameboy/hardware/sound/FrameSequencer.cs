using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class FrameSequencer {
    private int substep;
    private int step;

    private Channel[] channels;

    public int CurrentStep => step;

    public FrameSequencer(params Channel[] channels) {
        this.channels = channels;
    }

    public void Reset() {
        this.substep = 0;
        this.step = 0;
    }

    public void Step(ref ClockDelta dt) {
        const int clockCyclesPerStep = 8192;
        this.substep += dt.T;
        if (this.substep >= clockCyclesPerStep) {
            var steps = this.substep / clockCyclesPerStep;
            this.substep -= steps * clockCyclesPerStep;
            this.step = (step + steps) % 8;
            switch (this.step) {
                case 0:
                    ClockLength();
                    break;
                case 1:
                    break;
                case 2:
                    ClockLength();
                    ClockSweep();
                    break;
                case 3:
                    break;
                case 4:
                    ClockLength();
                    break;
                case 5:
                    break;
                case 6:
                    ClockLength();
                    ClockSweep();
                    break;
                case 7:
                    ClockVolumeEnvelope(); 
                    break;
            }
        }
    }

    public void ClockLength() {
        foreach (var chan in this.channels)
            chan.ClockLength(this.step);
    }

    public void ClockSweep() {
        foreach (var chan in this.channels)
            chan.ClockSweep(this.step);
    }

    public void ClockVolumeEnvelope() {
        foreach (var chan in this.channels)
            chan.ClockVolumeEnvelope(this.step);
    }

}