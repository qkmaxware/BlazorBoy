namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class CartridgeHeader {
    //http://gbdev.gg8.se/wiki/articles/The_Cartridge_Header

    public string title {get; init;}
    public RegionCode region {get; init;}
    public SgbSupport sgb {get; init;}
    public CgbSupport cgb {get; init;}
    public string manufacturerCode {get; init;}
    public int licenceeCode {get; init;}
    public LicenceeCode licencee {get; init;}
    public CartType cartType {get; init;}
    public int romSize {get; init;}
    public RomClass romClass {get; init;}
    public int ramSize {get; init;}
    public RamClass eramClass {get; init;}
    public int headerChecksum {get; init;}
    public int globalChecksum {get; init;}
    public int version {get; init;}

    public CartridgeHeader(byte[] romBank) {
        //Title
        string t = "Unknown";
        try{
            byte[] btt = new byte[16];
            for(int i = 0x0134, j=0; i <= 0x0143; i++, j++){
                var character = romBank[i];
                btt[j] = (character != '\0' ? character : (byte)' ');
            }
            t = System.Text.Encoding.ASCII.GetString(btt);
        }catch(Exception){}
        this.title = t.Trim();
        
        //Manufacturer's code (in some older cartridges)
        string mancode = "Unknown";
        try{
            byte[] btt = new byte[4];
            for(int i = 0x013F, j=0; i <= 0x0142; i++, j++){
                var character = romBank[i];
                btt[j] = (character != '\0' ? character : (byte)' ');
            }
            mancode = System.Text.Encoding.ASCII.GetString(btt);
        }catch(Exception){}
        this.manufacturerCode = mancode.Trim();
        
        //CGB Flag
        switch(romBank[0x0143]){
            case 0x80:
                this.cgb = CgbSupport.CbgAllowed;
                break;
            case 0xC0:
                this.cgb = CgbSupport.CbgRequired;
                break;
            default:
                this.cgb = CgbSupport.Unknown;
                break;
        }
        
        //New Licensee Code
        int ascii = (romBank[0x0144] << 8) | romBank[0x0145];
        this.licenceeCode = ascii;
        this.licencee = LicenceeCodeRegistry.Decode(ascii);
        
        //SGB Flag
        switch(romBank[0x0146]){
            case 0x00:
                this.sgb = SgbSupport.None;
                break;
            case 0x03:
                this.sgb = SgbSupport.SGB;
                break;
            default:
                this.sgb = SgbSupport.Unknown;
                break;
        }
        
        //Destination code
        this.region = RegionCodeRegistry.Decode(romBank[0x014A]);
        
        //Cart type
        this.cartType = CartType.Decode(romBank[0x0147]);
        
        //Rom size
        this.romClass = RomClass.Decode(romBank[0x148]);
        this.romSize = romBank[0x148];
        
        //Ram size
        this.ramSize = romBank[0x0149];
        this.eramClass = RamClass.Decode(romBank[0x0149]);
        
        //Rom version
        this.version = romBank[0x014C];
        
        //Header checksum
        this.headerChecksum = romBank[0x014D];
        
        //Global checksum
        this.globalChecksum = (romBank[0x014E] << 8) | romBank[0x014F];
    }

    public override string ToString() {
        return 
$@"Title:           {title}
Version:         {version}
Manufacturer:    {manufacturerCode}
Licencee:        {licencee}
Region:          {region}

Cart Type:       {cartType.MBC}
SGB Support:     {sgb}
CGB Support:     {cgb}
Rom:             {romClass.Size} bytes in {romClass.BankCount} banks
Ram:             {eramClass.Size} bytes in {eramClass.BankCount} banks

Header Checksum: {headerChecksum}
Global Checksum: {globalChecksum}
";
    }
}