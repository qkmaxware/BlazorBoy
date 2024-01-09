using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class RomOnlyMbc : IMbc {

    public static readonly int ERAM_SIZE = 32768;  //32KB eram
    private byte[] eram = new byte[ERAM_SIZE];    //External Cartridge RAM

    private Cartridge cart;

    public RomOnlyMbc(Cartridge cart){
        this.cart = cart;
    }

    public void Reset() {
        Array.Fill(eram, (byte)0);
    }

    public byte[] GetActiveRam() => eram;
    public IEnumerable<byte[]> GetRamBanks() {
        yield return GetActiveRam();
    }

    public int GetRamOffset() => 0;

    public int GetRomOffset() => 0x4000;

    private static bool between(int x, int lower, int upper) {
        return lower <= x && x <= upper;
    }

    public int ReadByte(int addr) {
        int romoff = GetRomOffset(); //Rom bank 1
        int ramoff = GetRamOffset();
        
        if(between(addr, 0, 0x3FFF)){
            //Cartridge ROM (fixed) (rom bank 0)
            if(cart == null)
                return 0;
            return cart.read(addr);
        }
        else if(between(addr, 0x4000, 0x7FFF)){
            //Cartridge ROM (switchable) (rom bank 1)
            if(cart == null)
                return 0;
            return cart.read((romoff + (addr&0x3FFF)));
        }
        else if(between(addr, 0xA000, 0xBFFF)){
            //External cartridge RAM
            return eram[ramoff + (addr&0x1FFF)]; //eram[ramoffs+(addr&0x1FFF)];
        }
        return 0;
    }

    public void WriteByte(int addr, int value) {
        //Create the appropriate offsets if required
        int romoff = GetRomOffset(); //Rom bank 1
        int ramoff = GetRamOffset();
        
        if(between(addr, 0xA000, 0xBFFF)){
            //External cartridge RAM
            eram[ramoff + (addr&0x1FFF)] = (byte)value; //eram[ramoffs+(addr&0x1FFF)];
        }
    }
}