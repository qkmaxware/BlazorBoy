using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// No MBC controller (ROM Only)
/// https://gbdev.io/pandocs/nombc.html
/// Small games of no more than 32KiB ROM do not require an MBC chip. 
/// </summary>
public class NoMbc : BaseMbc {

    private static readonly DataSize MaxRamSize = DataSize.Kibibytes(8);
    private byte[] eram = new byte[MaxRamSize.ByteCount]; 
    private bool usedOptionalRam = false;

    public NoMbc(Cartridge cart) : base(cart) {}

    public override void Reset() {
        // Copy from the cart, to the optional RAM
        for (var i = 0xA000; i < 0xBFFF; i++) {
            eram[i - 0xA000] = (byte)Cart.read(i);
        }
        usedOptionalRam = false;
    }

    public override byte[] GetActiveRam() => eram;
    public override IEnumerable<byte[]> GetRamBanks() {
        yield return GetActiveRam();
    }

    public override int ReadByte(int addr) {
        // The ROM is directly mapped to memory at $0000-7FFF
        if(between(addr, 0, 0x9FFF)){
            if(Cart is null)
                return LO;
            return Cart.read(addr);
        } 
        // Optionally up to 8 KiB of RAM could be connected at $A000-BFFF
        else if (between(addr, 0xA000, 0xBFFF)) {
            return eram[addr - 0xA000]; // is a duplicate of the cart due to the reset procedure
        }  
        // The ROM is directly mapped to memory at $0000-7FFF
        else if (between(addr, 0xC000, 0x7FFF)) {
            if(Cart is null)
                return LO;
            return Cart.read(addr);
        }
        return 0;
    }

    public override void WriteByte(int addr, int value) {
        // The ROM is directly mapped to memory at $0000-7FFF
        if(between(addr, 0, 0x9FFF)){
            // Read only
        } 
        // Optionally up to 8 KiB of RAM could be connected at $A000-BFFF
        else if (between(addr, 0xA000, 0xBFFF)) {
            usedOptionalRam = true;
            eram[addr - 0xA000] = (byte)value; 
        }  
        // The ROM is directly mapped to memory at $0000-7FFF
        else if (between(addr, 0xC000, 0x7FFF)) {
            // Read only
        }
    }
}