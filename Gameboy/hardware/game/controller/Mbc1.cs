namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Mbc1 : IMbc {
    public static readonly int ERAM_SIZE = 32768;  //32KB eram
    private byte[] eram = new byte[ERAM_SIZE];    //External Cartridge RAM
    
    private bool ramEnabled = false;
    private bool ramSelected = true;
    
    private int rambank = 0;
    private int rombank = 1;
 
    private Cartridge cart;

    public Mbc1(Cartridge cart){
        this.cart = cart;
    }

    public void Reset(){
        Array.Fill(eram, (byte)0);
        
        ramEnabled = false;
        ramSelected = true;
        
        rambank = 0;
        rombank = 1;
    }
    
    private static bool between(int x, int lower, int upper) {
        return lower <= x && x <= upper;
    }

    public bool IsRamEnabled(){
        return this.ramEnabled;
    }

    public int GetRamOffset() => rambank * 0x2000;

    public int GetRomOffset() => rombank * 0x4000;

    public byte[] GetActiveRam() => this.eram;

    public IEnumerable<byte[]> GetRamBanks() {
        yield return GetActiveRam();
    }

    public int ReadByte(int addr) {
        //Create the appropriate offsets if required
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
            return cart.read((romoff + (addr - 0x4000)));
        }
        else if(between(addr, 0xA000, 0xBFFF)){
            //External cartridge RAM
            if(!ramSelected){
                return eram[addr - 0xA000];
            }else{
                return eram[(addr - 0xA000) + ramoff];
            }
        }
        return 0;
    }

    public void WriteByte(int addr, int value) {
        //Create the appropriate offsets if required
        int romoff = GetRomOffset(); //Rom bank 1
        int ramoff = GetRamOffset();
        
        this.HasOccurredWrite(addr, value);
        
        if(between(addr, 0xA000, 0xBFFF)){
            //External cartridge RAM
            if(!ramSelected){
                eram[addr - 0xA000] = (byte)value;
            }else{
                eram[(addr - 0xA000) + ramoff] = (byte)value;
            }
        }
    }

    public void HasOccurredWrite(int addr, int value){
        if(addr >= 0x0000 && addr <= 0x1FFF){
            //Enable RAM. Any Value with 0x0AH in the lower 4 bits enables ram, other values disable ram
            ramEnabled = (value & 0x0F) == 0x0A;
        }else if(addr >= 0x2000 && addr <= 0x3FFF){
            if(!ramSelected){
                //If rammode not selected, this represents the lower 5 bits, preserve the upper 5 bits
                rombank = (value & 0x1F) | ((rombank >> 5) << 5);
            }else{
                rombank = value & 0x1F;
            }
            
            //Never select 0th rombank
            if(rombank == 0x00 || rombank == 0x20 || rombank == 0x40 || rombank == 0x60){
                rombank++;
            }
            
            rombank &= (cart.Info.romClass.BankCount - 1);
            
        }else if(addr >= 0x4000 && addr <= 0x5FFF){
            //This 2 bit register can be used to select a ram bank in the range 00-03 or specify the upper 2 bits of the bank number
            //This behavior depends on the ROM/RAM mode select
            if(!ramSelected){
                rombank = (rombank & 0x1F) | ((value & 3) << 5);   //Set upper 2 bits
                rombank &= (cart.Info.romClass.BankCount - 1);
            
            }else{
                rambank = value & 3;          //Set rambank number
                rambank &= (cart.Info.eramClass.BankCount - 1);
            }
        }
        else if(addr >= 6000 && addr <= 0x7FFF){
            //This one bit register selects whether the two bits above should be used as the upper two bits of the rom bank
            //or as the ram bank number
            ramSelected = (value & 0x1) != 0; //Ram banking mode, else Rom banking mode
        }
    }
}