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
        
        reg.Reset();
        cpp.Reset();
        clock.Reset();

        OnAfterReset();
    }

    protected virtual void OnBeforeReset() {

    }
    protected virtual void OnAfterReset() {

    }

    public int Step() {
        // Fetch intruction
        int address = reg.pc();
        OnBeforeFetch(address);
        int opcode = mem.ReadByte(address);
        OnAfterFetch(address, opcode);
        
        // Decode instruction
        var op = FetchOperation(opcode);
        OnAfterDecode(address, op);
        
        // Increment PC
        reg.pc(address + 1);
        
        // Execute instruction
        op.Invoke(); 
        
        // Increment the clock
        int deltaM = clock.delM();
        clock.Accept();

        // Interrupt handler
        if(reg.ime() != 0){
            HandleInterupts();
        }

        // Increment the clock in case interrupt has fired
        clock.Accept();
        OnAfterExecute(address, op);
    
        return deltaM;
    }
    protected virtual void OnBeforeFetch(int address) {

    }

    protected virtual void OnAfterFetch(int address, int opcode) {

    }

    protected virtual void OnAfterDecode(int address, Operation op) {

    }

    protected virtual void OnAfterExecute(int address, Operation op) {

    }


    protected virtual void HandleInterupts() {
        // Check the enabled flags, and what flags are triggered
        // Fire specific opcodes off
    }
}