namespace Qkmaxware.Emulators.Gameboy.Hardware;

public enum RegionCode {
    Unknown, Japanese, NotJapanese
}

public static class RegionCodeRegistry {
    public static RegionCode Decode(int headerValue){
        switch(headerValue){
            case 0x00:
                return RegionCode.Japanese;
            case 0x01:
                return RegionCode.NotJapanese;
            default:
                return RegionCode.Unknown;
        }
    }
}