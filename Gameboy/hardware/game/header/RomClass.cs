namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class RomClass {
    public int Size {get; private set;}
    public int BankCount {get; private set;}
    
    public RomClass(int kb, int banks){
        this.Size = kb;
        this.BankCount = banks;
    }
    
    public static RomClass Decode(int headerValue){
        switch(headerValue){
            case 0x00:
                return new RomClass(32, 2);
            case 0x01:
                return new RomClass(64, 4);
            case 0x02:
                return new RomClass(128, 8);
            case 0x03:
                return new RomClass(256, 16);
            case 0x04:
                return new RomClass(512, 32);
            case 0x05:
                return new RomClass(1000, 64);
            case 0x06:
                return new RomClass(2000, 128);
            case 0x07:
                return new RomClass(4000, 256);
            case 0x08:
                return new RomClass(8000, 512);
            case 0x52:
                return new RomClass(1100, 72);
            case 0x53:
                return new RomClass(1200, 80);
            case 0x54:
                return new RomClass(1500, 96);
            default:
                return new RomClass(0, 0);
        }
    }
}