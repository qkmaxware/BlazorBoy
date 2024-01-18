using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public partial class Ppu : IPpu { 
    public IEnumerable<ColourPallet[]> BackgroundPalettes {
        get {
            var palette = new ColourPallet[4];
            for (var i = 0; i < 4; i++) {
                palette[i] = this.BackgroundPalette[i];
            }
            yield return palette;
        }
    }
    public IEnumerable<ColourPallet[]> ObjectPalettes {
        get {
            var palette0 = new ColourPallet[4];
            var palette1 = new ColourPallet[4];
            for (var i = 0; i < 4; i++) {
                palette0[i] = this.Object0Palette[i];
                palette1[i] = this.Object1Palette[i];
            }
            yield return palette0;
            yield return palette1;
        }
    }
    public IEnumerable<SpriteSpan> Sprites => Array.AsReadOnly(sprites);
    public IEnumerable<TileSpan> Tiles => Array.AsReadOnly(tiles);
    public BackgroundMapSpan BackgroundMap => LCDC.BackgroundTileMapIndex == TileMapIndex.Map9800 ? Map9800 : Map9C00;
    public BackgroundMapSpan WindowMap => LCDC.WindowTileMapIndex == TileMapIndex.Map9800 ? Map9800 : Map9C00;
}