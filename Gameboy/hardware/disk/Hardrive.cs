using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

class Hardrive : IMemorySegment {
    public static readonly int WRAM_SIZE = 8192;
    public static readonly int ZRAM_SIZE = 128;  

    private int[] wram = new int[WRAM_SIZE]; //Working RAM
    private int[] zram = new int[ZRAM_SIZE]; //Zero page RAM

    

    public void Reset(){
        Array.Fill(wram, 0);
        Array.Fill(zram, 0);
    }

    private static bool between(int x, int lower, int upper) {
        return lower <= x && x <= upper;
    }

    public int ReadByte(int addr) {
        if(between(addr, 0xC000, 0xCFFF)){
            //Internal RAM (fixed)
            return wram[addr & 0x1FFF]; //wram[addr&0x1FFF];
        }
        else if(between(addr, 0xD000, 0xDFFF)){
            //Internal RAM (switchable)
            return wram[addr & 0x1FFF];
        }
        else if(between(addr, 0xE000, 0xFDFF)){
            //Shadow Interal RAM (seems wrong to me)
            return wram[addr & 0x1FFF];
        }
        else if(between(addr, 0xFF80, 0xFFFE)){
            //Zero page
            return zram[addr & 0x7F];   //zram[addr&0x7F];
        }
        return 0;
    }

    public void WriteByte(int addr, int value) {
        if(between(addr, 0xC000, 0xCFFF)){
            //Internal RAM (fixed)
            wram[addr & 0x1FFF] = value; //wram[addr&0x1FFF];
        }
        else if(between(addr, 0xD000, 0xDFFF)){
            //Internal RAM (switchable)
            wram[addr & 0x1FFF] = value;
        }
        else if(between(addr, 0xE000, 0xFDFF)){
            //Shadow Interal RAM (seems wrong to me)
            wram[addr & 0x1FFF] = value;
        }
        else if(between(addr, 0xFF80, 0xFFFE)){
            //Zero page
            zram[addr & 0x7F] = value;   //zram[addr&0x7F];
        }
    }

    public void SetMMU(MemoryMap mmu) { }


    

    
}