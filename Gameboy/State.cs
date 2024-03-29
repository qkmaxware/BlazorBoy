namespace Qkmaxware.Emulators.Gameboy;

public class CpuState {
    public int A {get; set;}
    public int B {get; set;}
    public int C {get; set;}
    public int D {get; set;}
    public int E {get; set;}
    public int F {get; set;}
    public int Hi {get; set;}
    public int Lo {get; set;}
    public int Sp {get; set;}
    public int Pc {get; set;}
    public int Ime {get; set;}
}

public class PpuState {
    public int Lcdc;
    public int Stat;
    public int ViewportX;
    public int ViewportY;
    public int WindowX;
    public int WindowY;
    public int Lyc;
    public int Scanline;

    public string? OamBytes;
    public string? VramBytes;
}

public class CartState {
    public string[]? RamBanks;
}

public class ConsoleState {
    public CpuState? Cpu {get; set;}
    public PpuState? Ppu {get; set;}
    public CartState? Cart {get; set;}
}