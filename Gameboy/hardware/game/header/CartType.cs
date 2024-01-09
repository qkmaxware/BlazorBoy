namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class CartType {
    public enum MBCtype {
        Unknown,
        ROM,
        MBC2,
        MBC1,
        MM01,
        MBC3,
        MBC5,
        MBC6,
        MBC7
    }

    public MBCtype MBC {get; private set;}
    public bool HasRam {get; private set;}
    public bool HasBattery {get; private set;}
        
    public CartType(MBCtype cont, bool ram, bool bat){
        this.MBC = cont;
        this.HasRam = ram;
        this.HasBattery = bat;
    }

    public static CartType Decode(int headerValue)
    {
        switch (headerValue)
        {
            case 0x08: //Rom + RAM
                return new CartType(MBCtype.ROM, true, false);
            case 0x09: //Rom + RAM + Battery
                return new CartType(MBCtype.ROM, true, true);
            case 0x00:
                return new CartType(MBCtype.ROM, false, false);
            case 0x02: //MBC1 + RAM
                return new CartType(MBCtype.MBC1, true, false);
            case 0x03: //MBC1 + RAM + Battery
                return new CartType(MBCtype.MBC1, true, true);
            case 0x01:
                return new CartType(MBCtype.MBC1, false, false);
            case 0x06: //MBC2 + Battery
                return new CartType(MBCtype.MBC2, true, true);
            case 0x05:
                return new CartType(MBCtype.MBC2, true, false);
            case 0x0C: //MM01 + RAM
                return new CartType(MBCtype.MM01, true, false);
            case 0x0D: //MM01 + RAM + Battery
                return new CartType(MBCtype.MM01, true, true);
            case 0x0B:
                return new CartType(MBCtype.MM01, false, false);
            case 0x0F: //MBC3 + TIMER + BATTERY
                return new CartType(MBCtype.MBC3, false, true);
            case 0x10: //MBC3 + TIMER + RAM + Battery
                return new CartType(MBCtype.MBC3, true, true);
            case 0x12: //MBC3 + RAM
                return new CartType(MBCtype.MBC3, true, false);
            case 0x13: //MBC3 + RAM + Battery
                return new CartType(MBCtype.MBC3, true, true);
            case 0x11:
                return new CartType(MBCtype.MBC3, false, false);
            case 0x1A: //MBC5 + RAM
                return new CartType(MBCtype.MBC5, true, false);
            case 0x1B: //MBC5 + RAM + Battery
                return new CartType(MBCtype.MBC5, true, true);
            case 0x1C: //MBC5 + Rumble
                return new CartType(MBCtype.MBC5, false, false);
            case 0x1D: //MBC5 + Rumble + RAM
                return new CartType(MBCtype.MBC5, true, false);
            case 0x1E: //MBC5 + Rumble + RAM + Battery
                return new CartType(MBCtype.MBC5, true, true);
            case 0x19:
                return new CartType(MBCtype.MBC5, false, false);
            case 0x20:
                return new CartType(MBCtype.MBC6, false, false);
            case 0x22:
                return new CartType(MBCtype.MBC7, false, false);
            default:
                return new CartType(MBCtype.Unknown, false, false);
        }
    }
}