using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// Conversion of https://github.com/qkmaxware/GBemu/blob/master/src/gameboy/gpu/Gpu.java
/// </summary>
public class Gpu : IPpu {
    
    public static readonly int VRAM_SIZE = 8192;
    public static readonly int OAM_COUNT = 40;
    public static readonly int LCD_HEIGHT = 144;
    public static readonly int LCD_WIDTH = 160;
    
    public static readonly int GPU_SCANLINEOAM = 2;    //10
    public static readonly int GPU_SCANLINEVRAM = 3;   //11
    public static readonly int GPU_HBLANK = 0;         //00
    public static readonly int GPU_VBLANK = 1;         //01
    
    public static readonly int TIME_SCANLINEOAM = 80;
    public static readonly int TIME_SCANLINEVRAM = 172;
    public static readonly int TIME_HBLANK = 204;
    public static readonly int TIME_FULLLINE = TIME_SCANLINEOAM + TIME_SCANLINEVRAM + TIME_HBLANK;
    public static readonly int TIME_VBLANK = 4560;
    public static readonly int TIME_FULLFRAME = TIME_FULLLINE*LCD_HEIGHT + TIME_VBLANK;

    #region References
    private MemoryMap? mmu;
    public void SetMMU(MemoryMap mmu) { this.mmu = mmu; }
    #endregion

    #region  Drawing
    public Bitmap Canvas {get; private set;} = new Bitmap(LCD_WIDTH, LCD_HEIGHT);
    private Bitmap buffer = new Bitmap(LCD_WIDTH, LCD_HEIGHT);
    private ColourPallet[] bgpallet = new ColourPallet[4];
    private ColourPallet[] obj0pallet = new ColourPallet[4];
    private ColourPallet[] obj1pallet = new ColourPallet[4];

    private int[] vram = new int[VRAM_SIZE];
    private int[,,] tilemap = new int[512, 8, 8];
    #endregion

    #region OAM
    private int[] oam = new int[OAM_COUNT * 4];
    private Sprite[] oam_data = new Sprite[OAM_COUNT];
    private Sprite[] oam_data_sorted = new Sprite[OAM_COUNT];
    #endregion

    private Dictionary<int, int> reg = new Dictionary<int, int>();
    private int yscroll = 0;
    private int xscroll = 0;
    private int ywindow = 0;
    private int xwindow = 0;
    private int curline = 0;
    private int lyc = 0;
    private int gpumode = 2;
    private int clock = 0;
    
    public bool windowon = false;
    public bool lcdon = false;
    public bool largeobj = false;
    public bool objon = false;
    public bool bgon = false;
    public bool coincidenceInterruptEnable = false;
    public bool oamInterruptEnable = false;
    public bool vblankInterruptEnable = false;
    public bool hblankInterruptEnable = false;
    
    public bool useLargerWindowTileStartAddress = false;    //False = 0x1800, true = unknown
    public bool useSmallerTileStartAddress = false; //false = 0x0000;  true = 0x0800
    public bool useLargerTileMapStartAddress = false;  //false = 0x1800;  true = 0x1C00

    public Gpu() {
        Array.Fill(oam, 0);
        for(int i = 0; i < oam_data.Length; i++){
            oam_data[i] = new Sprite(i);
            oam_data_sorted[i] = oam_data[i];
        }
        
        Reset();
    }

    public void Reset() {
        //Drawing board
        Canvas.Fill(ColourPallet.BackgroundWhite);
        
        //Tilemap
        for (int i = 0; i < 512; i++) {
            for (int j = 0; j < 8; j++) {
                for (int k = 0; k < 8; k++) {
                    tilemap[i,j,k] = 0;
                }
            }
        }

        //VRAM
        Array.Fill(vram, 0);
        
        //Reset OAM
        Array.Fill(oam, 0);
        for(int i = 0; i < oam_data.Length; i++){
            oam_data[i].Reset();
        }

        //Colour Pallet
        Array.Fill(bgpallet, ColourPallet.BackgroundWhite);
        Array.Fill(obj0pallet, ColourPallet.Object0White);
        Array.Fill(obj1pallet, ColourPallet.Object1White);
        
        //Internal values
        yscroll = 0;
        xscroll = 0;
        curline = 0;
        lyc = 0;
        gpumode = 2;
        ywindow = 0;
        xwindow = 0;
        
        windowon = false;
        lcdon = false;
        largeobj = false;
        objon = false;
        bgon = false;
    
        useLargerWindowTileStartAddress = false;
        useSmallerTileStartAddress = false;
        useLargerTileMapStartAddress = false;
        
        reg.Clear();
        
        //LCD status setup
        gpumode = GPU_VBLANK;
        clock = 0;
        
        coincidenceInterruptEnable = false;
        oamInterruptEnable = false;
        vblankInterruptEnable = false;
        hblankInterruptEnable = false;

        reg.Add(0xFF41, 0b10000000 | getLcdStatus());

        HasBufferJustFlushed = false;
    }

    public bool IsInVBlank => gpumode == Gpu.GPU_VBLANK;
    public bool IsInHBlank => gpumode == Gpu.GPU_HBLANK;
    public void Step(int step) {
        HasBufferJustFlushed = false;
        
        //If LCD is off, treat it as a perminent VBLANK
        if(!lcdon){
            curline = 0;
            gpumode = Gpu.GPU_VBLANK;
            return;
        }

        clock += step;  //Step is in m time not in cycles (t)
        int oldmode = gpumode;

        switch(gpumode){
            //In HBlank
            case 0:
                if(clock >= TIME_HBLANK / 4){
                    //end of hblank, for last scanline render screen
                    if(curline == 143){
                        gpumode = Gpu.GPU_VBLANK;
                        mmu?.RequestInterrupt(MemoryMap.INTERRUPT_VBLANK);
                        flushBuffer();
                    } else {
                        gpumode = Gpu.GPU_SCANLINEOAM;
                    }
                    
                    curline++;
                    clock = 0;
                }
                break;
            //In VBlank
            case 1:
                if(clock >= 114){ //Worth 10 lines
                    clock = 0;
                    curline++;
                    if(curline > 153){
                        curline = 0;
                        gpumode = Gpu.GPU_SCANLINEOAM;  
                    }
                }
                break;
            //In OAM-read mode
            case 2:
                if(clock >= TIME_SCANLINEOAM / 4){
                    clock = 0;
                    gpumode = Gpu.GPU_SCANLINEVRAM;
                }
                break;
            //VRAM-read mode
            case 3:
                //Render scanline at end of allotted time
                if(clock >= TIME_SCANLINEVRAM / 4){
                    clock = 0;
                    gpumode = Gpu.GPU_HBLANK;
                    renderScanline();
                }
                break;
        }
        
        //Mode had changed, do I need to fire an interrupt
        if(oldmode != this.gpumode){
            //Moved onto starting to draw the next line's OAM stage
            if(gpumode == Gpu.GPU_SCANLINEOAM){
                if(this.oamInterruptEnable){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                } 
            }
            //Moved onto starting to draw the next line's VRAM stage
            else if(gpumode == Gpu.GPU_SCANLINEVRAM){
                if(this.coincidenceInterruptEnable && this.curline == this.lyc){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                }
            }
            //Finished a line
            else if(gpumode == Gpu.GPU_HBLANK){
                if(this.hblankInterruptEnable){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                }
            }
            //Finished drawing the screen
            else if(gpumode == Gpu.GPU_VBLANK){
                if(this.vblankInterruptEnable){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                }
            }
        }
    }

    public int GetWindowMapStartAddress(){ //Bit 6
        return useLargerWindowTileStartAddress ? 0x1C00 : 0x1800;
    }
    
    public int GetBgMapStartAddress(){ //Bit 3
        return useLargerTileMapStartAddress ? 0x1C00 : 0x1800;
    }
    
    public int GetBgTileStartAddress(){ //Bit 4
        return useSmallerTileStartAddress ? 0x0000 : 0x0800;
    }
    
    public bool IsBgTileAddressRegionUnsigned(int tileaddress){
        return tileaddress !=0 ? false : true;
    }

    public int readVRAM(int idx){
        return this.vram[idx];
    }

    public int readTile(int tile, int y, int x){
        return this.tilemap[tile,y,x];
    }

    private int getLcdStatus(){
        int result = gpumode;           //Mode flag
        if(curline == lyc){             //LYC=LY flag
            result |= 0b100;
        }
        if(hblankInterruptEnable){      //Mode 0 interrupt enabled
            result |= 0b1000;
        }
        if(vblankInterruptEnable){      //Mode 1 interrupt enabled
            result |=  0b10000;
        }
        if(oamInterruptEnable){         //Mode 2 interrupt enabled
            result |= 0b100000;
        }
        if(coincidenceInterruptEnable){ //LYC = LY interrupt enabled
            result |= 0b1000000;
        }
        //What about bit 7?
        if(reg.ContainsKey(0xFF41)){    //Preserve saved bit 7
            result |= reg[0xFF41] & 0b10000000;
        }
        
        return result; //(curline == lyc ? 4 : 0) | gpumode; //Instead of result. See jsGB
    }

    private void setLcdStatus(int value){
        //Extract out bit-pattern
        coincidenceInterruptEnable = (value & 0x40) == 0x40;
        oamInterruptEnable = (value & 0x20) == 0x20;
        vblankInterruptEnable = (value & 0x10) == 0x10;
        hblankInterruptEnable = (value & 0x08) == 0x08;
        
        //If lcd is not on, set the mode to 1 and reset the scanline
        if(!lcdon){
            curline = 0;
            gpumode = Gpu.GPU_VBLANK;
            return;
        }
        
        int currentMode = gpumode;
        int desiredMode = (value & 0x03);
        
        var requireInterrupt = false;
        if(currentMode == Gpu.GPU_VBLANK){
            requireInterrupt = vblankInterruptEnable;
        } else if(currentMode == Gpu.GPU_HBLANK){
            requireInterrupt = hblankInterruptEnable;
        } else if(currentMode == Gpu.GPU_SCANLINEOAM){
            requireInterrupt = oamInterruptEnable;
        }
        
        //Just entered a new mode, request interrupt
        if(requireInterrupt && (currentMode != desiredMode)){
            if (mmu is not null)
                mmu.InteruptFlags |= 2;   //Bit 2, LCDC interrupt
        }
        
        //Check the coincidence flag
        if(curline == lyc){
            if(coincidenceInterruptEnable)
                if (mmu is not null)
                    mmu.InteruptFlags |= 2;
        }
        
        //Finally, set the gpu mode
        gpumode = desiredMode;
    }

    public void renderScanline(){
        int[] scanrow = new int[LCD_WIDTH];
        
        if(!lcdon){ 
            return;
        }
        
        if(bgon){
            renderBackgroundLine(this.curline, scanrow);
        }
        
        if(objon){
            renderSpriteLine(this.curline, scanrow);
        }
    }

    public void renderBackgroundLine(int scanline, int[] scanrow) { 
        //Scroll Y is the Y position of the background where to start drawing the viewing area from
        //Scroll X is the X position of the background where to start drawing the viewing area from
        //Window Y is the Y position of the viewing area to start drawing the window from
        //Window X is the X position (-7) of the viewing area to start drawing the window from
        int scrollY = this.yscroll;
        int scrollX = this.xscroll;
        int windowY = this.ywindow;
        int windowX = this.xwindow - 7;
        int pixelY = scanline;
        
        //Are we using a window?
        var usewindow = false;
        if(this.windowon){
            if(windowY <= scanline){
                usewindow = true;
            }
        }
        
        //Get address space for tiles
        int tileAddressBase = GetBgTileStartAddress();
        var unsigned = IsBgTileAddressRegionUnsigned(tileAddressBase);
        
        //What background memory
        int backgroundAddressBase;
        if(!usewindow){
            backgroundAddressBase = GetBgMapStartAddress();
        }else{
            backgroundAddressBase = GetWindowMapStartAddress();
        }
        
        //Which of the 32 vertical tiles am I drawing
        int tileY = scrollY + scanline;
        if(usewindow){
            tileY = scanline - windowY;
        }
        
        //Which of the 8 vertical pixels of this tile am I on
        int rowY = ((tileY & 255) / 8) * 32;
        
        //Time to start drawing the scanline
        for(int pixelX = 0; pixelX < buffer.Width; pixelX++){
            int xpos = pixelX + scrollX;
            
            //Translate into window space
            if(usewindow){
                if(pixelX >= windowX){
                    xpos = pixelX - windowX;
                }
            }
            
            //Which of the 32 horizontal tiles does this pixel fall
            int tileX = (xpos / 8) & 31;
            
            //Get the tile id number
            int tileNum = vram[backgroundAddressBase + rowY + tileX];
            if(!unsigned){
                //Signed space, tile address != 0
                if(tileNum < 128)
                    tileNum = 256+tileNum;
            }
            
            //Obtain the tile from the tilenumber => Get the pixel
            var colorNum = tilemap[tileNum, tileY % 8, xpos % 8];
            var c = this.bgpallet[colorNum];
            
            //Set the pixel in the buffer and in the scanline
            scanrow[pixelX] = colorNum;
            buffer[pixelX, pixelY] = c;
        }
    }

    public void renderSpriteLine(int scanline, int[] scanrow){
        int curline = scanline;
        int pixelY = curline;

        int spritesize = (largeobj ? 16 : 8);

        for(int i = 0; i < this.oam_data.Length; i++){   //40 Sprites
            Sprite spr = this.oam_data_sorted[i];

            // Does the sprite land on the scanline
            if(spr.y <= curline && (spr.y + spritesize) > curline) {
                // For large sprites the last bit is always 0
                int tile = (largeobj ? spr.tile & (0xFE) : spr.tile);

                // If y flipped, grab the opposite horizontal row
                int yind;
                if(spr.yflip == Sprite.YOrientation.Flipped){
                    yind = (spritesize - 1)-(curline - spr.y);
                }else {
                    yind = curline - spr.y;
                }

                //Get the tile row
                //either the selected tile or the next tile for large sprites with y in the range (8-15)
                var tilemap0 = (yind > 7 ? tile + 1 : tile);
                var tilemap1 = (yind > 7 ? yind - 8 : yind);

                //Select the color pallet to use
                ColourPallet[] pallet;
                if(spr.objPalette == Sprite.Palette.Zero){
                    pallet = this.obj0pallet;
                }else{
                    pallet = this.obj1pallet;
                }

                // Draw sprite
                for(int x = 0; x < 8; x++) {
                    int pixelX = (spr.x + x); //This is wrong - xscroll

                    // Pixel must be on the screen to be drawn
                    if(!(pixelX >= 0 && pixelX < scanrow.Length))
                        continue;

                    // Flip x coordinate if required
                    int xpos = (spr.xflip == Sprite.XOrientation.Flipped) ? 7-x : x;
                    int colorNum = tilemap[tilemap0, tilemap1, xpos];

                    // Only if not WHITE (aka alpha) AND
                    // Only if priority is above background or the background is white
                    if(colorNum != 0 && (spr.priority == Sprite.Priority.AboveBackground || scanrow[pixelX] == 0)) {
                        var c = pallet[colorNum];
                        buffer[pixelX, pixelY] = c;
                    }
                }
            }
        }
    }

    public bool HasBufferJustFlushed {get; private set;}
    public void flushBuffer(){
        var temp = this.buffer;
        this.buffer = Canvas;
        this.Canvas = temp;
        this.buffer.Fill(ColourPallet.BackgroundWhite);
        // Swap is faster than actually copying pixels
        /*for(int x = 0; x < Canvas.Width; x++){
            for(int y = 0; y < Canvas.Height; y++){
                Canvas[x, y] = buffer[x, y];
                buffer[x, y] = ColourPallet.BackgroundWhite;
            }
        }*/
        HasBufferJustFlushed = true;
    }

    public int ReadByte(int addr)
    {
        //VRAM
        if(addr >= 0x8000 && addr <= 0x9FFF){
            return vram[addr&0x1FFF];
        }

        //OAM
        if(addr >= 0xFE00 && addr <= 0xFE9F){
            return oam[addr & 0xFF];
        }
        
        //Registers
        switch(addr){
            case 0xFF40:    //LCD Control
                return (lcdon ? 0x80 : 0) | 
                        (largeobj ? 0x04 : 0) | 
                        (objon ? 0x02 : 0) | 
                        (bgon ? 0x01 : 0) | 
                        ((useSmallerTileStartAddress) ? 0x10 : 0) | 
                        ((useLargerTileMapStartAddress) ? 0x08 : 0) |
                        (windowon ? 0x20 : 0) |
                        (useLargerWindowTileStartAddress? (0x40) : 0);
            case 0xFF41:    //LCD Status
                return getLcdStatus();
            case 0xFF42:    //Scroll Y         
                return yscroll;
            case 0xFF43:    //Scroll X         
                return xscroll;
            case 0xFF44:    //Current scanline
                return curline;
            case 0xFF45:    //Raster?
                return lyc;
            case 0xFF47:    //Background Pallet
            case 0xFF48:    //Object Pallet 0
            case 0xFF49:    //Object Pallet 1
            case 0xFF4A:    //Window Y
                return ywindow;
            case 0xFF4B:    //Window X
                return xwindow;
            default:
                if(reg.ContainsKey(addr))
                    return reg[addr];
                return 0;
        }
    }

    public bool IsLcdOn => lcdon;

    public void WriteByte(int addr, int value)
    {
        //VRAM
        if(addr >= 0x8000 && addr <= 0x9FFF){
            vram[addr&0x1FFF] = value;
            updatetile(addr&0x1FFF, value);
            return;
        }
        
        //OAM
        if(addr >= 0xFE00 && addr <= 0xFE9F){
            oam[addr & 0xFF] = value;
            updateoam(addr, value);
            return;
        }
        
        reg[addr] = value;
        switch(addr){
            case 0xFF40:    //LCD Control
                lcdon = (value & 0x80) != 0;                            //BIT 7 - LCD Display (1=on, 0=off)
                useLargerWindowTileStartAddress = (value & 0x40) != 0;  //BIT 6 - Window Tile Map Display (0=9800-9BFF, 1=9C00-9FFF)
                windowon = (value & 0x20) != 0;                         //BIT 5 - Window Tile Display Enable (0=off, 1=on)
                useSmallerTileStartAddress = ((value&0x10) != 0);       //BIT 4 - BG Tile Data Select (0=8800-97FF, 1=8000-8FFF)
                useLargerTileMapStartAddress = ((value&0x08) != 0);     //BIT 3 - BG Tile Map Select (0=9800-9BFF, 1=9C00-9FFF)
                largeobj = (value&0x04) != 0;                           //BIT 2 - Sprite Size (0=8x8, 1=8x16)
                objon = (value&0x02) != 0;                              //BIT 1 - Sprite Display (0=off, 1=on)
                bgon = (value&0x01) != 0;                               //BIT 0 - BG Display (0=off, 1=on)
                break;
            case 0xFF41:    //LCD Status
                setLcdStatus(value);
                break;
            case 0xFF42:    //Scroll Y         
                yscroll = value;
                break;
            case 0xFF43:    //Scroll X         
                xscroll = value;
                break;
            case 0xFF44:    //Current scanline
                curline = value;
                break;
            case 0xFF45:    //Raster?
                lyc = value;
                break;
            case 0xFF46:    //Object Attribute Memory OAM Direct Data Transfer
                for(int i = 0; i < 160; i++){
                    int v = mmu?.ReadByte((value << 8) + i) ?? 0;
                    oam[i] = v;
                    updateoam(0xFE00 + i, v);
                }
                break;
            case 0xFF47:    //Background Pallet
                for(int i = 0; i < 4; i++){
                    int v = (value >> (i * 2)) & 3; //1,2,3
                    switch(v){
                        case 0: bgpallet[i] = ColourPallet.BackgroundWhite; break;
                        case 1: bgpallet[i] = ColourPallet.BackgroundLight; break;
                        case 2: bgpallet[i] = ColourPallet.BackgroundMedium; break;
                        case 3: bgpallet[i] = ColourPallet.BackgroundDark; break;
                    }
                }
                break;
            case 0xFF48:    //Object Pallet 0
                for(int i = 0; i < 4; i++){
                    int v = (value >> (i * 2)) & 3; //1,2,3
                    switch(v){
                        case 0: obj0pallet[i] = ColourPallet.Object0White; break;
                        case 1: obj0pallet[i] = ColourPallet.Object0Light; break;
                        case 2: obj0pallet[i] = ColourPallet.Object0Medium; break;
                        case 3: obj0pallet[i] = ColourPallet.Object0Dark; break;
                    }
                }
                break;
            case 0xFF49:    //Object Pallet 1
                for(int i = 0; i < 4; i++){
                    int v = (value >> (i * 2)) & 3; //1,2,3
                    switch(v){
                        case 0: obj1pallet[i] = ColourPallet.Object1White; break;
                        case 1: obj1pallet[i] = ColourPallet.Object1Light; break;
                        case 2: obj1pallet[i] = ColourPallet.Object1Medium; break;
                        case 3: obj1pallet[i] = ColourPallet.Object1Dark; break;
                    }
                }
                break;
            case 0xFF4A:    //Window Y
                ywindow = value;
                break;
            case 0xFF4B:    //Window X
                xwindow = value;
                break;
        }
    }

    protected void updatetile(int addr, int value){
        int saddr = addr;
        if((addr & 1) != 0){
            saddr --;
            addr --;
        }
        
        int tile = (addr >> 4) & 511;
        int y = (addr >> 1) & 7;
        
        int sx;
        for(int x = 0; x < 8; x++){
            sx = 1 << (7 - x);
            tilemap[tile,y,x] = ((vram[saddr] & sx) != 0 ? 1 : 0) | ((vram[saddr + 1] & sx) != 0 ? 2 : 0);
        }
    }

    protected void updateoam(int addr, int value) {
        addr -= 0xFE00;
        int obj = addr >> 2; //Divide by 4
        if (obj > 40 || obj < 0) {
            return;
        }
        
        switch (addr & 3) {
            case 0:
                oam_data[obj].y = value - 16;
                break;
            case 1:
                oam_data[obj].x = value - 8;
                break;
            case 2:
                if (largeobj) {
                    oam_data[obj].tile = (value & 0xFE);
                } else {
                    oam_data[obj].tile = value;
                }
                break;
            case 3:
                oam_data[obj].priority = ((value & 0x80) != 0 ? Sprite.Priority.BelowBackground : Sprite.Priority.AboveBackground );
                oam_data[obj].xflip = ((value & 0x20) != 0 ? Sprite.XOrientation.Flipped : Sprite.XOrientation.Normal);
                oam_data[obj].yflip = ((value & 0x40) != 0 ? Sprite.YOrientation.Flipped : Sprite.YOrientation.Normal);
                oam_data[obj].objPalette = ((value & 0x10) != 0 ? Sprite.Palette.One : Sprite.Palette.One);
                break;
        }

	    //Resort sorted oam_data;
        Array.Sort(oam_data_sorted, spriteSorter);
    }

    private SpriteSorter spriteSorter = new SpriteSorter();
    private class SpriteSorter : IComparer<Sprite> {
        public int Compare(Sprite? x, Sprite? y) {
            if (x is null && y is null)
                return 0;
            if (x is null)
                return 1;
            if (y is null)
                return -1;

            if(x.x > y.x) {
                return -1;
            } else if (x.x == y.x) {
                if (x.id > y.id) {
                    return -1;
                } else if (x.id == y.id) {
                    return 0;
                } else {
                    return 1;
                }
            } else {
                return 1;
            }
        }
    }
}