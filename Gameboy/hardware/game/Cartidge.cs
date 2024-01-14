namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Cartridge {

    public CartridgeHeader Info {get; init;}
    private byte[] rom;

    public Cartridge(byte[] rom){
        this.Info = new CartridgeHeader(rom);
        this.rom = rom;
    }

    public IEnumerable<byte> Bytes => Array.AsReadOnly(rom);

    public bool supportsSGB(){
        return Info.sgb == SgbSupport.SGB;
    }
    
    public bool supportsCGB(){
        return (Info.cgb == CgbSupport.CbgAllowed || Info.cgb == CgbSupport.CbgRequired);
    }
    
    public bool HasRam(){
        return Info.cartType.HasRam;
    }
    
    public int read(int addr){
        if (addr >= 0 && addr < rom.Length) {
            return rom[addr];
        } else {
            return 0;
        }
    }
}