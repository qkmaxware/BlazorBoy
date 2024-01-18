using System.Globalization;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Mbc3 : IMbc {

    private class RTC{
        public int s;
        public int m;
        public int h;
        public int day;
        public bool halt = false;
        public bool carry = false;
        
        public int rtcRegister = 0x08;
        
        public RTC(){
            LatchCurrent();
        }
        
        public void LatchCurrent(){
            var now = DateTime.Now;
            s = now.Second   & 0xFF;
            m = now.Minute   & 0xFF;
            h = now.Hour     & 0xFF;
            day = (int)now.DayOfWeek;
        }
        
        public void Reset(){
            rtcRegister = 0x08;
            halt = false;
            carry = false;
            LatchCurrent();
        }
        
        public int rb(){
            switch(rtcRegister){
                case 0x08: //Seconds
                    return this.s;
                case 0x09: //Minutes
                    return this.m;
                case 0x0A: //Hours
                    return this.h;
                case 0x0B: //Lower 8 bits of the day
                    return this.day & 0xFF;
                case 0x0C: //Upper bit of day plus flags in bits 6 and 7
                    return ((this.day >> 8) & 0b1) | (halt ? 0x40 : 0) | (carry ? 0x80 : 0);
            }
            return 0xFF;
        }
        
        public void wb(int value){
            switch(rtcRegister){
                case 0x08: //Seconds
                    this.s = value & 0xFF;
                    break;
                case 0x09: //Minutes
                    this.m = value & 0xFF;
                    break;
                case 0x0A: //Hours
                    this.h = value & 0xFF;
                    break;
                case 0x0B: //Lower 8 bits of the day
                    this.day &= ~(0xFF);
                    this.day |= (value & 0xFF);
                    break;
                case 0x0C: //Upper 8 bits of day plus flags
                    this.day &= 0xFF;
                    this.day |= (value >> 8) & 0xFF;
                    
                    this.halt = (value & 0x40) != 0;
                    this.carry = (value & 0x80) != 0;
                    break;
            }
        }
    }

    public static readonly int ERAM_SIZE = 32768;  //32KB eram
    private byte[] eram = new byte[ERAM_SIZE];    //External Cartridge RAM
    
    private RTC rtc = new RTC();
    private int rombank = 1;
    private int rambank = 0;
    private bool ramEnabled = false;
    private bool rtcEnabled = false;
    private bool rtcReadEnabled = false;
    private bool rtcLatchNext = false;
    
    private Cartridge cart;

    public Mbc3(Cartridge cart){
        this.cart = cart;
    }

    public void Reset(){
        Array.Fill(eram, (byte)0);
        rtc.Reset();
        
        ramEnabled = false;
        rtcLatchNext = false;
        rtcReadEnabled = false;
        rtcEnabled = false;
        
        rambank = 0;
        rombank = 1;
    }

    private static bool between(int x, int lower, int upper) {
        return lower <= x && x <= upper;
    }

    public int GetRamOffset(){
        return rambank * 0x2000;
    }
    public int GetRomOffset(){
        return rombank * 0x4000;
    }
    public bool IsRtcOn(){
        return rtcEnabled;
    }
    public byte[] GetActiveRam() => this.eram;

    public IEnumerable<byte[]> GetRamBanks() {
        yield return GetActiveRam();
    }
    public void UpdateRamBanks(IEnumerable<byte[]> banks) => throw new NotImplementedException();

    public int ReadByte(int addr)
    {
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
            return cart.read(romoff + (addr - 0x4000));
        }
        else if(between(addr, 0xA000, 0xBFFF)){
            //External cartridge RAM OR RTC based on the mode
            if(!rtcReadEnabled && ramEnabled){
                return eram[ramoff + (addr - 0xA000)]; 
            }else if(IsRtcOn()){
                return rtc.rb();
            }
        }
        return 0;
    }

    public void WriteByte(int addr, int value)
    {
        //Create the appropriate offsets if required
        int romoff = GetRomOffset(); //Rom bank 1
        int ramoff = GetRamOffset();
        
        this.HasOccurredWrite(addr, value);
        
        if(between(addr, 0xA000, 0xBFFF)){
            //External cartridge RAM or RTC registers
            if(this.rtcReadEnabled){
                if(IsRtcOn())
                    rtc.wb(value);
            }
            else if(ramEnabled){
                eram[ramoff + (addr - 0xA000)] = (byte)value; //eram[ramoffs+(addr&0x1FFF)];
            }
        }
    }

    public void HasOccurredWrite( int addr, int value) {
        //Ram and timer enable
        if(addr >= 0 && addr <= 0x1FFF){
            //A value of 0x0A will enable reading and writing to ram and to the RTC, 00 will diable both
            ramEnabled = cart.HasRam() && (value & 0x0F) == 0x0A;
            rtcEnabled = (value & 0x0F) == 0x0A;
        }
        //Rom bank number
        else if(addr >= 0x2000 && addr <= 0x3FFF){
            value &= 0x7F;
            if(value <= 0)
                value = 1;
            
            rombank = value;
            rombank &= (cart.Info.romClass.BankCount - 1);
        }
        //Ram bank number - or - RTC select register
        else if(addr >= 0x4000 && addr <= 0x5FFF){
            //Value in range 0x00-0x03 maps the RAM bank to A000, 0x08-0x0C maps the RTC 
            value &= 0xFF;
            
            if(value >= 00 && value <= 0x03){
                //Map rombank to address 0xA000 to 0xBFFF
                this.rtcReadEnabled = false;
                this.rambank = value;
                this.rambank &= (cart.Info.eramClass.BankCount - 1);
            }else if(value >= 0x08 && value <= 0x0C){
                //Map RTC register to address 0xA000 to 0xBFFF
                this.rtcReadEnabled = true;
                this.rtc.rtcRegister = value;
            }
        }
        //Latch clock data
        else if(addr >= 0x6000 && addr <= 0x7FFF){
            //Writing a 0 then a 1 to this register the current time becomes latched to the RTC register
            //Latched data will not change until latched again
            if(this.rtcLatchNext && (value & 0xFF) == 1){
                rtc.LatchCurrent();
                rtcLatchNext = false;
            }
            this.rtcLatchNext = (value == 0x00);
        }
    }
}