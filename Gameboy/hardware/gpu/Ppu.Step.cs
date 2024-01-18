using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public partial class Ppu : IPpu {

    private int clock = 0;

    public static readonly int TIME_SCANLINEOAM = 80;
    public static readonly int TIME_SCANLINEVRAM = 172;
    public static readonly int TIME_HBLANK = 204;
    public static readonly int TIME_FULLLINE = TIME_SCANLINEOAM + TIME_SCANLINEVRAM + TIME_HBLANK;
    public static readonly int TIME_VBLANK = 4560;
    public static readonly int TIME_FULLFRAME = TIME_FULLLINE*LCD_HEIGHT + TIME_VBLANK;

    public void Step(int step) {
        ResetStepFlags();

        //If LCD is off, treat it as a permanent VBLANK
        if(!LCDC.IsLcdEnabled){
            ScanLineIndex = 0;
            clock = 0;
            STAT.Mode = PpuMode.VBlank;
            return;
        }

        clock += step;  //Step is in m time not in cycles (t)
        var oldmode = STAT.Mode;

        switch(oldmode){
            //In HBlank
            case PpuMode.HBlank:
                if(clock >= TIME_HBLANK / 4){
                    //end of hblank, for last scanline render screen
                    if(ScanLineIndex == 143){
                        STAT.Mode = PpuMode.VBlank;
                        mmu?.RequestInterrupt(MemoryMap.INTERRUPT_VBLANK);
                        flushBuffer();
                    } else {
                        STAT.Mode = PpuMode.OamScan;
                    }
                    
                    ScanLineIndex++;
                    clock = 0;
                }
                break;
            //In VBlank
            case PpuMode.VBlank:
                if(clock >= 114){ //Worth 10 lines
                    clock = 0;
                    ScanLineIndex++;
                    if(ScanLineIndex > 153){
                        ScanLineIndex = 0;
                        STAT.Mode = PpuMode.OamScan;  
                    }
                }
                break;
            //In OAM-read mode
            case PpuMode.OamScan:
                if(clock >= TIME_SCANLINEOAM / 4){
                    clock = 0;
                    STAT.Mode = PpuMode.Drawing;
                }
                break;
            //VRAM-read mode
            case PpuMode.Drawing:
                //Render scanline at end of allotted time
                if(clock >= TIME_SCANLINEVRAM / 4){
                    clock = 0;
                    STAT.Mode = PpuMode.HBlank;
                    renderScanline();
                }
                break;
        }

        //Mode had changed, do I need to fire an interrupt
        var newmode = this.STAT.Mode;
        if(oldmode != newmode){
            //Moved onto starting to draw the next line's OAM stage
            if(newmode == PpuMode.OamScan){
                if(this.STAT.IsOamScanInterruptEnabled){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                } 
            }
            //Moved onto starting to draw the next line's VRAM stage
            else if(newmode == PpuMode.Drawing){
                if(this.STAT.IsLycLyInterruptEnabled && this.ScanLineIndex == this.LYC){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                }
            }
            //Finished a line
            else if(newmode == PpuMode.HBlank){
                if(this.STAT.IsHBlankInterruptEnabled){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                }
            }
            //Finished drawing the screen
            else if(newmode == PpuMode.VBlank){
                if(this.STAT.IsVBlankInterruptEnabled){
                    mmu?.RequestInterrupt(MemoryMap.INTERRUPT_LCDC);
                }
            }
        }
    }

}