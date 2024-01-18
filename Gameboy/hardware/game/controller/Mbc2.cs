namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Mbc2 : IMbc {
    private Cartridge cart;
    
    private int rombank = 1;
    private bool ramEnabled = false;
    
    public static readonly int ERAM_SIZE = 512;
    private byte[] eram = new byte[ERAM_SIZE];

    public Mbc2(Cartridge cart){
        this.cart = cart;
    }

    public void Reset() {
        rombank = 1;
        ramEnabled = false;
        
        Array.Fill(eram, (byte)0);
    }

    public int GetRamOffset() {
        return 0x2000;
    }

    public int GetRomOffset() {
        return rombank * 0x4000;
    }

    public byte[] GetActiveRam(){
        return this.eram;
    }
    public IEnumerable<byte[]> GetRamBanks() {
        yield return GetActiveRam();
    }

    public void UpdateRamBanks(IEnumerable<byte[]> banks) => throw new NotImplementedException();

    public int ReadByte(int addr) {
        //ROM BANK 0 (Read Only)
        if(addr >= 0x0000 && addr <= 0x3FFF){
            return cart.read(addr);
        }
        //ROM BANK 1-F (Read Only)
        else if(addr >= 0x4000 && addr <= 0x7FFF){
            return cart.read((addr - 0x4000) + GetRomOffset());
        }
        //512x4bit RAM (RW)
        else if(addr >= 0xA000 && addr <= 0xA1FF){
            if(this.ramEnabled)
                return this.eram[addr - 0xA000];
            else
                return 0xFF;
        } 
        
        return 0;
    }

    public void WriteByte(int addr, int value) {
         //RAM ENABLE (Write Only)
        if(addr >= 0x0000 && addr <= 0x1FFF){
            //Least significant bit of the upper address byte must be 0 to enable/disable ram cart
            if((addr & 0x100) == 0){
                ramEnabled = ((value & 0x0F) == 0x0A);
            }
        }
        //ROM BANK NUMBER (Write Only)
        else if(addr >= 0x2000 && addr <= 0x3FFF){
            //Least significant bit of the upper address byte must be 1 to select a rom bank
            if((addr & 0x100) != 0){
                this.rombank = value & 0x0F;
                this.rombank &= (this.cart.Info.romClass.BankCount - 1);
                if(this.rombank == 0)
                    this.rombank = 1;
            }
        }
        //512x4bit RAM (RW)
        else if(addr >= 0xA000 && addr <= 0xA1FF){
            if(ramEnabled){
                this.eram[addr - 0xA000] = (byte)(value & 0x0F);
            }
        }
    }
}