namespace Qkmaxware.Vm.LR35902;

/// <summary>
/// LR35902 (Z80) emulated CPU
/// </summary>
public partial class Cpu : IResetable {
    protected Registry reg {get; private set;}
    protected Registry cpp {get; private set;}
    protected Clock clock {get; private set;}
    protected IMemory mem {get; private set;}

    public Registry Registry => reg;
    public Clock Clock => clock;

    private Operation[] map;
    private Operation[] cbmap;

    public Cpu(IMemory mem) {
        this.reg = new Registry();
        this.cpp = new Registry();
        this.clock = new Clock();
        this.mem = mem;

        this.map = new Operation[256];
        this.cbmap = new Operation[256];

        this.rebuildOperationMap();
    }

    public void Reset() {
        OnBeforeReset();
        
        isHalted = false;
        reg.Reset();
        cpp.Reset();
        clock.Reset();

        OnAfterReset();
    }

    protected virtual void OnBeforeReset() {

    }
    protected virtual void OnAfterReset() {

    }

    protected virtual int RegisterIE() => 0;
    protected virtual int RegisterIF() => 0;
    public int Step() {
        // Interrupt handler
        if ((RegisterIF() & RegisterIE()) != 0) {
            if (reg.ime() != 0) {
                if (IsInHaltMode()) {
                    push(haltModeEnabledAt); // Push the instruction after the halt
                } else {
                    push(reg.pc());          // Push current pc
                }
                HandleInterrupts();
                reg.ime(0);
                int deltaMInterrupt = clock.delM();
                clock.Accept();
                return deltaMInterrupt;
            }
            ExitHaltMode();
        }

        if (IsInHaltMode()) {
            return 1;
        }

        // Fetch instruction
        int address = reg.pc();
        OnBeforeFetch(address);
        int opcode = mem.ReadByte(address);
        OnAfterFetch(address, opcode);

        // Increment PC
        reg.pc(address + 1);
        
        // Decode instruction
        var op = FetchOperation(opcode);
        var args = op.Arity > 0 ? new int[op.Arity] : noArgs; 
        for (var i = 0; i < args.Length; i++) {
            switch (op.GetArgumentType(i)) {
                case ArgT.Short:
                    args[i] = mem.ReadShort(reg.pc());
                    reg.pcpp(2);
                    break;
                case ArgT.Byte:
                default:
                    args[i] = mem.ReadByte(reg.pc());
                    reg.pcpp(1);
                    break;
            }
        }
        OnAfterDecode(address, op);
        
        // Execute instruction
        op.Invoke(args); 
        OnAfterExecute(address, op);

        // Increment the clock
        int deltaM = clock.delM();
        clock.Accept();

        // Increment the clock in case interrupt has fired
        clock.Accept();
    
        return deltaM;
    }
    private bool isHalted = false;
    private int haltModeEnabledAt = 0;
    public bool IsInHaltMode() => isHalted;
    public void EnterHaltMode() {
        // file:///D:/CS_Projects/BlazorBoy/Private/TCAGBD.pdf 
        // Page 16
        if (!IsInHaltMode()) {
            this.isHalted = true;
            haltModeEnabledAt = reg.pc();
        }
    }
    public void ExitHaltMode() {
        this.isHalted = false;
    }
    protected virtual void OnBeforeFetch(int address) {

    }

    protected virtual void OnAfterFetch(int address, int opcode) {

    }

    protected virtual void OnAfterDecode(int address, Operation op) {

    }

    protected virtual void OnAfterExecute(int address, Operation op) {

    }


    protected virtual void HandleInterrupts() {
        // Check the enabled flags, and what flags are triggered
        // Fire specific opcodes off
    }
}