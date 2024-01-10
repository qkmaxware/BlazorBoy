using System.Diagnostics;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Cpu : Qkmaxware.Vm.LR35902.Cpu {

    private MemoryMap mmu => (MemoryMap)mem;

    public ITrace? Trace {get; set;}

    private Operation RST_40h;
    private Operation RST_48h;
    private Operation RST_50h;
    private Operation RST_58h;
    public Operation RST_60h;

    public Cpu(MemoryMap map) : base(map) {
        this.RST_40h = new Operation(0xFF1, "RST 40H", () => {
            rst(0x40);
        });
        
        this.RST_48h = new Operation(0xFF2, "RST 48H", () => {
            rst(0x48);
        });
        
        this.RST_50h = new Operation(0xFF3, "RST 50H", () => {
            rst(0x50);
        });
        
        this.RST_58h = new Operation(0xFF4, "RST 58H", () => {
            rst(0x58);
        });
        
        this.RST_60h = new Operation(0xFF5, "RST 60H", () => {
            rst(0x60);
        });
    }

    protected override void OnAfterReset() {
        // Initial values expected by the bios
        reg.af((mmu.GetMappedComponent(MemoryMap.ROM_BANK_0) != null && ((CartridgeAdapter)mmu.GetMappedComponent(MemoryMap.ROM_BANK_0)).SupportsCGB()) ? 0x11B0 : 0x01B0);
        reg.bc(0x0013);
        reg.de(0x00D8);
        reg.hl(0x014D);
        
        reg.sp(0xFFFE);
        reg.pc(0x0100);
        
        mmu.WriteByte(0xFF05, 0x00);   //TIMA
        mmu.WriteByte(0xFF06, 0x00);   //TMA
        mmu.WriteByte(0xFF07, 0x00);   //TAC
        mmu.WriteByte(0xFF10, 0x80);   //NR10
        mmu.WriteByte(0xFF11, 0xBF);   //NR11
        mmu.WriteByte(0xFF12, 0xF3);   //NR12
        mmu.WriteByte(0xFF14, 0xBF);   //NR14
        mmu.WriteByte(0xFF16, 0x3F);   //NR21
        mmu.WriteByte(0xFF17, 0x00);   //NR22
        mmu.WriteByte(0xFF19, 0xBF);   //NR24
        mmu.WriteByte(0xFF1A, 0x7F);   //NR30
        mmu.WriteByte(0xFF1B, 0xFF);   //NR31
        mmu.WriteByte(0xFF1C, 0x0F);   //NR32
        mmu.WriteByte(0xFF1E, 0xBF);   //NR33
        mmu.WriteByte(0xFF20, 0xFF);   //NR41
        mmu.WriteByte(0xFF21, 0x00);   //NR42
        mmu.WriteByte(0xFF22, 0x00);   //NR43
        mmu.WriteByte(0xFF23, 0xBF);   //NR30
        mmu.WriteByte(0xFF24, 0x77);   //NR50
        mmu.WriteByte(0xFF25, 0xF3);   //NR51
        mmu.WriteByte(0xFF26, 0xF1);   //NR52 
        mmu.WriteByte(0xFF40, 0x91);   //LCDC -- Also turns on the LCD
        mmu.WriteByte(0xFF42, 0x00);   //SCY
        mmu.WriteByte(0xFF43, 0x00);   //SCX
        mmu.WriteByte(0xFF45, 0x00);   //LYC
        mmu.WriteByte(0xFF47, 0xFC);   //BGP
        mmu.WriteByte(0xFF48, 0xFF);   //0BP0
        mmu.WriteByte(0xFF49, 0xFF);   //0BP1
        mmu.WriteByte(0xFF4A, 0x00);   //WY
        mmu.WriteByte(0xFF4B, 0x00);   //WX
        mmu.WriteByte(0xFFFF, 0x00);   //EI
    }

    public PerformanceAnalyzer? PerformanceAnalyzer {get; set;}
    private PerformanceAnalyzer.ActivePerformanceMeasure? measure;
    protected override void OnBeforeFetch(int address) {
        measure = PerformanceAnalyzer?.BeginMeasure(null);
    }
    protected override void OnAfterExecute(int address, Operation op) {
        measure?.ChangeKey(op)?.Record();
        Trace?.Add(address, op);
    }

    protected override void HandleInterupts() {
        if (mmu.EnabledInterupts == 0 || mmu.InteruptFlags == 0) {
            return;
        }

        //Mask off ints that arent enabled
        int fired = mmu.EnabledInterupts & mmu.InteruptFlags;
        
        //INTERRUPT TABLE-------------------------------
        // Interrupt        ISR Address     Bit Value
        // Vblank           0x40            0
        // LCD stat         0x48            1
        // Timer            0x50            2
        // Serial           0x58            3
        // Joypad Press     0x60            4   
        //----------------------------------------------
        
        if((fired & MemoryMap.INTERRUPT_VBLANK) != 0){
            //Vblank
            mmu.InteruptFlags &= 0xFE;
            RST_40h.Invoke();
        }
        if((fired & MemoryMap.INTERRUPT_LCDC) != 0){
            //LCDC stat
            mmu.InteruptFlags &= 0xFD;
            RST_48h.Invoke();
        }
        if((fired & MemoryMap.INTERRUPT_TIMEROVERFLOW) != 0){
            //Timer
            mmu.InteruptFlags &= 0xFB;
            RST_50h.Invoke();
        }
        if((fired & MemoryMap.INTERRUPT_SERIAl) != 0){
            //Serial
            mmu.InteruptFlags &= 0xF7;
            RST_58h.Invoke();
        }
        if((fired & MemoryMap.INTERRUPT_JOYPAD) != 0){
            //Joypad press
            mmu.InteruptFlags &= 0xEF;
            RST_60h.Invoke();
        }
    }
}
