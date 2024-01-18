using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public partial class Ppu : IDebuggablePpu { 
    /// <summary>
    /// Height of a Game Boy LCD screen in pixels
    /// </summary>
    public static readonly int LCD_HEIGHT = 144;
    /// <summary>
    /// Width of a Game Boy LCD screen in pixels
    /// </summary>
    public static readonly int LCD_WIDTH = 160;
    /// <summary>
    /// Number of tiles possible in the VRAM
    /// </summary>
    public static readonly int TILE_COUNT = 512;
    /// <summary>
    /// Size of the Video RAM (VRAM) in bytes
    /// </summary>
    public static readonly int VRAM_SIZE = TILE_COUNT * 16; // 8192
    /// <summary>
    /// Number of objects in the Object Attribute Memory
    /// </summary>
    public static readonly int OAM_COUNT = 40;
    /// <summary>
    /// Size of the Object Attribute Memory in bytes
    /// </summary>
    public static readonly int OAM_SIZE = OAM_COUNT * 4;

    /// <summary>
    /// Canvas containing the rendered image
    /// </summary>
    public Bitmap Canvas {get; private set;} = new Bitmap(LCD_WIDTH, LCD_HEIGHT);
    /// <summary>
    /// buffer to be written to before a vblank
    /// </summary>
    private Bitmap buffer {get; set;} = new Bitmap(LCD_WIDTH, LCD_HEIGHT);

    /// <summary>
    /// Test if the PPU's buffer has flushed last step
    /// </summary>
    public bool HasBufferJustFlushed {get; private set;}

    private MemoryMap? mmu;
    public void SetMMU(MemoryMap mmu) { this.mmu = mmu; }

    private LcdControlRegister LCDC = new LcdControlRegister();
    private LcdStatusRegister STAT = new LcdStatusRegister();
    private PaletteRegister BackgroundPalette = new PaletteRegister(ColourPallet.BackgroundDark, ColourPallet.BackgroundMedium, ColourPallet.BackgroundLight, ColourPallet.BackgroundWhite);
    private PaletteRegister Object0Palette  = new PaletteRegister(ColourPallet.Object0Dark, ColourPallet.Object0Medium, ColourPallet.Object0Light, ColourPallet.Object0White);
    private PaletteRegister Object1Palette  = new PaletteRegister(ColourPallet.Object1Dark, ColourPallet.Object1Medium, ColourPallet.Object1Light, ColourPallet.Object1White);
    int ScanLineIndex = 0;
    int LYC = 0;
    private IntCoordinate Viewport = new IntCoordinate();
    private IntCoordinate Window = new IntCoordinate();
    private byte[] vram = new byte[VRAM_SIZE];
    private TileSpan[] tiles = new TileSpan[TILE_COUNT];
    private BackgroundMapSpan Map9800;
    private BackgroundMapSpan Map9C00;
    private byte[] oam = new byte[OAM_SIZE];
    private SpriteSpan[] sprites = new SpriteSpan[OAM_COUNT];
    private SpriteSpan[] sprites_sorted = new SpriteSpan[OAM_COUNT];

    public Ppu() {
        for (var i = 0; i < sprites.Length; i++) {
            var sprite = new SpriteSpan(i, i*4, this.oam);
            sprites[i] = sprite;
            sprites_sorted[i] = sprite;
        }
        for (var i = 0; i < tiles.Length; i++) {
            var tile = new TileSpan(0x8000, i, this.vram);
            tiles[i] = tile;
        }
        Map9800 = new BackgroundMapSpan(0x9800, 0x9800 - 0x8000, this.vram);
        Map9C00 = new BackgroundMapSpan(0x9C00, 0x9C00 - 0x8000, this.vram);
    }

    public void Reset() {
        clock = 0;
        LYC = 0;
        ScanLineIndex = 0;
        LCDC.Value = 0;
        STAT.Value = 133;

        BackgroundPalette.Value = 0;
        Object0Palette.Value = 0;
        Object1Palette.Value = 0;

        Viewport.X = 0; Viewport.Y = 0;
        Window.X = 0; Window.Y = 0;

        Array.Fill(vram, (byte)0);
        Array.Fill(oam, (byte)0);

        Canvas.Fill(ColourPallet.BackgroundWhite);

        // Reset flags
        ResetStepFlags();
    }

    private void ResetStepFlags() {
        HasBufferJustFlushed = false;
    }

    public int ReadByte(int addr) {
        // VRAM
        if(addr >= 0x8000 && addr <= 0x9FFF){
            return this.vram[addr&0x1FFF];
        }

        //OAM
        if(addr >= 0xFE00 && addr <= 0xFE9F){
            return this.oam[addr & 0xFF];
        }
        
        // Registers
        switch (addr) {
            case 0xFF40:
                return this.LCDC.Value;
            case 0xFF41:
                return this.STAT.Value;
            case 0xFF42:    //Scroll Y         
                return this.Viewport.Y;
            case 0xFF43:    //Scroll X         
                return this.Viewport.X;
            case 0xFF44:    //Current scanline
                return this.ScanLineIndex;
            case 0xFF45:    //Raster?
                return this.LYC;
            case 0xFF47:    //Background Pallet
                return this.BackgroundPalette.Value;
            case 0xFF48:    //Object Pallet 0
                return this.Object0Palette.Value;
            case 0xFF49:    //Object Pallet 1
                return this.Object1Palette.Value;
            case 0xFF4A:    //Window Y
                return this.Window.Y;
            case 0xFF4B:    //Window X
                return this.Window.X;
            default:
                return 0;
        }
    }

    public void WriteByte(int addr, int value) {
        //VRAM
        if(addr >= 0x8000 && addr <= 0x9FFF){
            this.vram[addr&0x1FFF] = (byte)value;
            return;
        }
        
        //OAM
        if(addr >= 0xFE00 && addr <= 0xFE9F){
            this.oam[addr & 0xFF] = (byte)value;
            refreshOam();
            return;
        }

        // Registry
        switch(addr){
            case 0xFF40:    //LCD Control
                this.LCDC.Value = value;
                break;
            case 0xFF41:    //LCD Status
                this.STAT.Value = value;
                break;
            case 0xFF42:    //Scroll Y         
                this.Viewport.Y = value;
                break;
            case 0xFF43:    //Scroll X         
                this.Viewport.X = value;
                break;
            case 0xFF44:    //Current scanline
                this.ScanLineIndex = value;
                break;
            case 0xFF45:    //Raster?
                this.LYC = value;
                break;
            case 0xFF46:    //Object Attribute Memory OAM Direct Data Transfer
                for(int i = 0; i < 160; i++){
                    int v = mmu?.ReadByte((value << 8) + i) ?? 0;
                    this.oam[i] = (byte)v;
                }
                refreshOam();
                break;
            case 0xFF47:    //Background Pallet
                this.BackgroundPalette.Value = value;
                break;
            case 0xFF48:    //Object Pallet 0
                this.Object0Palette.Value = value;
                break;
            case 0xFF49:    //Object Pallet 1
                this.Object1Palette.Value = value;
                break;
            case 0xFF4A:    //Window Y
                this.Window.Y = value;
                break;
            case 0xFF4B:    //Window X
                this.Window.X = value;
                break;
        }
    }

    private void refreshOam() {
        Array.Sort(sprites_sorted, spriteSorter);
    }

    private SpriteSorter spriteSorter = new SpriteSorter();
    private class SpriteSorter : IComparer<SpriteSpan> {
        public int Compare(SpriteSpan? x, SpriteSpan? y) {
            if (x is null && y is null)
                return 0;
            if (x is null)
                return 1;
            if (y is null)
                return -1;

            if(x.XRight > y.XRight) {
                return -1;
            } else if (x.XRight == y.XRight) {
                if (x.Id > y.Id) {
                    return -1;
                } else if (x.Id == y.Id) {
                    return 0;
                } else {
                    return 1;
                }
            } else {
                return 1;
            }
        }
    }

    public PpuState GetState() {
        PpuState state = new PpuState();
        state.Lcdc = this.ReadByte(0xFF40);
        state.Stat = this.ReadByte(0xFF41);
        state.ViewportX = this.ReadByte(0xFF43);
        state.ViewportY = this.ReadByte(0xFF42);
        state.WindowX = this.ReadByte(0xFF4B);
        state.WindowY = this.ReadByte(0xFF4A);
        state.Lyc = this.ReadByte(0xFF45);
        state.Scanline = this.ReadByte(0xFF44);
           
        state.OamBytes = Convert.ToBase64String(this.oam);
        state.VramBytes = Convert.ToBase64String(this.vram);

        return state; 
    }
}