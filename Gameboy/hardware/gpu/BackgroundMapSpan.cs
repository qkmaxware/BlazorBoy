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

    protected int unsignedByteToSigned(int u8){
        if(u8 > 127)
            u8 = -((~u8+1)&255);
        return u8;
    }
    public int TileIndexAt(int x, int y, TileDataSelect mode = TileDataSelect.Method8000) {
        // Row by row
        var raw_index = VramReference[StartIndex + (x + Width * y)];
        if (mode == TileDataSelect.Method8000) {
            return raw_index;
        } else {
            return unsignedByteToSigned(raw_index);
        }
    }

    public TileSpan TileAt(int x, int y, TileDataSelect mode = TileDataSelect.Method8000) {
        var index = TileIndexAt(x, y, mode);
        if (mode == TileDataSelect.Method8000) {
            return new TileSpan(0x8000, index, this.VramReference);
        } else {
            return new TileSpan(0x8000, 256 + index, this.VramReference);
        }
    }

    public Bitmap ToBitmap(TileDataSelect mode = TileDataSelect.Method8000) {
        var bitmap = new Bitmap(256, 256);
        var index = 0;
        for (var row = 0; row < 32; row++) {
            for (var col = 0; col < 32; col++) {
                // Only works for method 1 0x8000 method (TODO method 2)
                TileSpan tile = TileAt(col, row, mode);
                var stamp = tile.ToBitmap();
                bitmap.Stamp(col * 8, row * 8, stamp);
                index++;
            }
        }
        return bitmap;
    }
}