namespace Qkmaxware.Vm.LR35902;

/// <summary>
/// Amount of time passed when the CPU is stepped
/// </summary>
public struct ClockDelta {
    public int T { get; private set; }
    public int InstructionCycles => T;

    public int M { get; private set; }
    public int MachineCycles => M;

    public ClockDelta (int t, int m) {
        this.T = t;
        this.M = m;
    }
}