namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class BackgroundMapSpan {
    
    /// <summary>
    /// The base address in the MMU
    /// </summary>
    public int AddressBase {get; private set;}
    /// <summary>
    /// The starting index in the vram of this background map
    /// </summary>
    public int StartIndex {get; private set;}
    /// <summary>
    /// The ending index in the vram for this background map
    /// </summary>
    public int EndIndex => StartIndex + 1024; // 32 * 32 bytes

    /// <summary>
    /// Width of the background map
    /// </summary>
    public static readonly int Width = 32;
    /// <summary>
    ///  Height of the background map
    /// </summary>
    public static readonly int Height = 32;

    private byte[] VramReference;

    public BackgroundMapSpan(int addressBase, int startIndex, byte[] vram) {
        this.AddressBase = addressBase;
        this.StartIndex = startIndex;
        this.VramReference = vram;
    }

    public byte TileIndexAt(int x, int y) {
        // Row by row
        return VramReference[StartIndex + (x + Width * y)];
    }

    public Bitmap ToBitmap() {
        var bitmap = new Bitmap(256, 256);
        var index = 0;
        for (var row = 0; row < 32; row++) {
            for (var col = 0; col < 32; col++) {
                // Only works for method 1 0x8000 method (TODO method 2)
                var tileIndex = this.TileIndexAt(col, row);
                TileSpan tile = new TileSpan(0x8000, tileIndex, this.VramReference);
                var stamp = tile.ToBitmap();
                bitmap.Stamp(col * 8, row * 8, stamp);
                index++;
            }
        }
        return bitmap;
    }
}