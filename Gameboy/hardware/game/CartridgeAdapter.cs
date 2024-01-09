namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class CartridgeAdapter : IMemorySegment {
    private Cartridge? cart;
    private IMbc? controller;

    public CartridgeAdapter() {

    }

    public Cartridge? LoadedCart() => cart;
    public bool HasCart() => cart is not null && controller is not null;

    public void Reset() {
        if(this.controller != null){
            this.controller.Reset();
        }
    }

    

    public void LoadCart(Cartridge cart){
        this.cart = cart;
        
        switch(cart.Info.cartType.MBC){
            case CartType.MBCtype.MBC1:
                controller = (IMbc) new Mbc1(cart);
                break;
            case CartType.MBCtype.MBC2:
                controller = (IMbc) new Mbc2(cart);
                break;
            case CartType.MBCtype.MBC3:
                controller = (IMbc) new Mbc3(cart);
                break;
            case CartType.MBCtype.MBC5:
                controller = (IMbc) new Mbc5(cart);
                break;
            default:
                controller = (IMbc) new RomOnlyMbc(cart);
                break;
        }
    }

    public void LoadRam(params byte[][] banks) {
        if(this.cart is null || this.controller is null || !cart.Info.cartType.HasBattery)
            return;
        
        throw new NotImplementedException();
    }

    public byte[][] DumpRam() {
        if(this.cart is null || this.controller is null || !cart.Info.cartType.HasBattery)
            return new byte[0][];
        
        return this.controller.GetRamBanks().ToArray();
    }

    public int ReadByte(int address) {
        if(this.controller is not null){
            return this.controller.ReadByte(address);
        }
        return 0;
    }

    public void WriteByte(int address, int value) {
        if(this.controller is not null){
            this.controller.WriteByte(address, value);
        }
    }

    public bool SupportsCGB(){
        if(cart == null)
            return false;
        return cart.supportsCGB();
    }

   public void SetMMU(MemoryMap mmu) { }

    public int ReadShort(int address)
    {
        throw new NotImplementedException();
    }

    public void WriteShort(int address, int value)
    {
        throw new NotImplementedException();
    }
}