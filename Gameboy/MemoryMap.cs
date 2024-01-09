using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy;

// https://github.com/qkmaxware/GBemu/blob/master/src/gameboy/MemoryMap.java
public class MemoryMap : IMemory {
    //GB memory map is as follows
    //http://gbdev.gg8.se/wiki/articles/Main_Page
    /*
        FFFF            Interrupt Enable Flag
        FF80 - FFFE     Zero page (127bytes)
        FF00 - FF7F     Hardware IO Registers
        FEA0 - FEFF     Unusable memory 
        FE00 - FE9F     OAM (object attribute memory)
        E000 - FDFF     Shadow Internal RAM
        D000 - DFFF     Internal RAM (switchable)
        C000 - CFFF     Internal RAM (fixed)
        A000 - BFFF     External Cartridge RAM
        9C00 - 9FFF     Video RAM (background map data 2)
        9800 - 9BFF     Video RAM (background map data 1)
        8000 - 97FF     Video RAM (character ram)
        4000 - 7FFF     Cartridge ROM (switchable)
        0150 - 3FFF     Cartridge ROM (fixed)
        0100 - 014F     Cartridge Header
        0000 - 00FF     Restart and Interrupt Vectors
    */
    
    #region Memory Mapping
    public static readonly int ROM_BANK_0 = 0;
    public static readonly int ROM_BANK_1 = 1;
    public static readonly int VRAM = 2;
    public static readonly int EXTERNAL_RAM = 3;
    public static readonly int INTERNAL_RAM = 4;
    public static readonly int OAM = 5;
    public static readonly int GPU = 6;
    public static readonly int ZRAM = 7;
    public static readonly int JOYSTICK = 9;
    public static readonly int TIMER = 10;
    public static readonly int SERIALIO = 8;

    private IMemorySegment[] mappedComponents = new IMemorySegment[11];
    public IMemorySegment GetMappedComponent(int i){
        return this.mappedComponents[i];
    }
    
    public void SetMappedComponent(int i, IMemorySegment map){
        this.mappedComponents[i] = map;
        map.SetMMU(this);
    }
    #endregion

    #region Interupts
    public int EnabledInterupts = 0;
    public int InteruptFlags = 0;
    public static readonly int INTERRUPT_VBLANK = 0b1;
    public static readonly int INTERRUPT_LCDC = 0b10;
    public static readonly int INTERRUPT_TIMEROVERFLOW = 0b100;
    public static readonly int INTERRUPT_SERIAl = 0b1000;
    public static readonly int INTERRUPT_JOYPAD = 0b10000;

    public void RequestInterrupt(int interrupt){
        this.InteruptFlags |= interrupt;
        this.InteruptFlags &= 0xFF;
    }
    
    public bool EnableInterrupt(int interrupt){
        return  (this.EnabledInterupts & (~interrupt) & 0xFF) != 0;
    }
    #endregion

    public int MaxAddress => 0xFFFF;

    public void Reset() {
        foreach(var comp in this.mappedComponents){
            if(comp is not null){
                comp.Reset();
            }
        }
    }

    private static bool between(int x, int lower, int upper) {
        return lower <= x && x <= upper;
    }

    public int ReadByte(int addr) {
        if(between(addr, 0x0000, 0x3FFF)){
           //Rom Bank 0
           return mappedComponents[ROM_BANK_0].ReadByte(addr);
       }
       else if(between(addr, 0x4000, 0x7FFF)){
           //Rom Bank 1
           return mappedComponents[ROM_BANK_1].ReadByte(addr);
       }
       else if(between(addr, 0x8000, 0x9FFF)){
           //Video Ram
           return mappedComponents[VRAM].ReadByte(addr);
       }
       else if(between(addr, 0xA000, 0xBFFF)){
           //Cartridge Ram
           return mappedComponents[EXTERNAL_RAM].ReadByte(addr);
       }
       else if(between(addr, 0xC000, 0xFDFF)){
           //Work Ram and Shadow
           return mappedComponents[INTERNAL_RAM].ReadByte(addr);
       }
       else if(between(addr, 0xFE00, 0xFE9F)){
           //Object Attribute Memory
           return mappedComponents[OAM].ReadByte(addr);
       }
       else if(between(addr, 0xFF80, 0xFFFE)){
           //Zero Page Ram
           return mappedComponents[ZRAM].ReadByte(addr);
       }
       else if(addr == 0xFF00){
           //Joystick input
           return mappedComponents[JOYSTICK].ReadByte(addr);
       }
       else if(between(addr, 0xFF01, 0xFF03)){
           //Serial IO Data, Control, UNKNOWN
           return 0;
       }
       else if(between(addr, 0xFF04, 0xFF0E)){
           //Timer
           return mappedComponents[TIMER].ReadByte(addr);
       }
       else if(addr == 0xFF0F){
           //Interrupt Flags
           return InteruptFlags;
       }
       else if(between(addr, 0xFF10, 0xFF39)){
           //Sound control, envelope ect
           return 0;
       }
       else if(between(addr, 0xFF40, 0xFF7F)){
           return mappedComponents[GPU].ReadByte(addr);
       }
       else if(addr == 0xFFFF){
           return EnabledInterupts;
       }
       return 0;
    }

    public int ReadShort(int address) {
        return (ReadByte(address + 1) << 8) | ReadByte(address);
    }

    public void WriteByte(int addr, int value) {
        value &= 0xFF;
        
        if(between(addr, 0x0000, 0x3FFF)){
           //Rom Bank 0 -- Readonly
           mappedComponents[ROM_BANK_0].WriteByte(addr, value);    //Read Only Skip
        }
        else if(between(addr, 0x4000, 0x7FFF)){
            //Rom Bank 1 -- Readonly
            mappedComponents[ROM_BANK_1].WriteByte(addr, value);    //Read Only Skip
        }
        else if(between(addr, 0x8000, 0x9FFF)){
            //Video Ram
            mappedComponents[VRAM].WriteByte(addr, value);
        }
        else if(between(addr, 0xA000, 0xBFFF)){
            //Cartridge Ram
            mappedComponents[EXTERNAL_RAM].WriteByte(addr, value); //WORKING I WROTE TO THIS
        }
        else if(between(addr, 0xC000, 0xFDFF)){
            //Work Ram and Shadow
            mappedComponents[INTERNAL_RAM].WriteByte(addr, value);
        }
        else if(between(addr, 0xFE00, 0xFE9F)){
            //Object Attribute Memory
            mappedComponents[OAM].WriteByte(addr, value);
        }
        else if(between(addr, 0xFF80, 0xFFFE)){
            //Zero Page Ram
            mappedComponents[ZRAM].WriteByte(addr, value);
        }
        else if(addr == 0xFF00){
            //Joystick input
            mappedComponents[JOYSTICK].WriteByte(addr, value);
        }
        else if(between(addr, 0xFF01, 0xFF03)){
            if(addr == 0xFF01 && mappedComponents[SERIALIO] != null){ //Serial IO Data
                mappedComponents[SERIALIO].WriteByte(addr, value);
            }
            //Control, UNKNOWN
        }
        else if(between(addr, 0xFF04, 0xFF0E)){
            //Timer
            mappedComponents[TIMER].WriteByte(addr, value);
        }
        else if(addr == 0xFF0F){
            //Interrupt Flags
            InteruptFlags = value;
       }
       else if(between(addr, 0xFF10, 0xFF39)){
           //Sound control, envelope ect
       }
       else if(between(addr, 0xFF40, 0xFF7F)){
           mappedComponents[GPU].WriteByte(addr, value);
       }
       else if(addr == 0xFFFF){
           EnabledInterupts = value;
       }
    }

    public void WriteShort(int address, int value) {
        WriteByte(address,value&255); 
        WriteByte(address+1,value>>8);
    }
}