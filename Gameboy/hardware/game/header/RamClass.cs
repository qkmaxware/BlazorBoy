namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class RamClass {
    
    public int Size {get; private set;}
    public int BankCount {get; private set;}
    
    public RamClass(int size, int banks){
        this.Size = size;
        this.BankCount = banks;
    }
    
    public static RamClass Decode(int headerValue){
        switch(headerValue){
            case 0x01:
                return new RamClass(2, 1);
            case 0x02:
                return new RamClass(8, 1);
            case 0x03:
                return new RamClass(32, 4);
            case 0x04:
                return new RamClass(128, 16);
            case 0x05:
                return new RamClass(64, 8);
            case 0x00:
            default:
                return new RamClass(0, 0);
        }
    }
    
}