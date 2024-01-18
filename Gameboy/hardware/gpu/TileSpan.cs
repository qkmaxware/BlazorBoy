namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// An 8px by 8px tile renderable by the PPU
/// </summary>
public class TileSpan {
    /// <summary>
    /// Base address for the tile's tilemap
    /// </summary>
    public int BaseAddress {get; private set;}
    /// <summary>
    /// The index to the tile in a linear array
    /// </summary>
    public int TileIndex {get; private set;}
    /// <summary>
    /// The actual address of the start of the tile
    /// </summary>
    public int TileAddress => BaseAddress + TileIndex * 16;
    /// <summary>
    /// A reference to the Vram byte array
    /// </summary>
    private byte[] VramReference {get; init;}

    /// <summary>
    /// Tile width
    /// </summary>
    public static readonly int Width = 8;
    /// <summary>
    /// Tile height
    /// </summary>
    public static readonly int Height = 8;

    public TileSpan (int baseAddr, int tileIndex, byte[] vram) {
        this.BaseAddress = baseAddr;
        this.TileIndex = tileIndex;
        this.VramReference = vram;
    }

    private static readonly byte Black = 3;
    private static readonly byte White = 0;
    private static readonly byte LightGrey = 1;
    private static readonly byte DarkGrey = 2;

    public byte this [int x, int y] {
        get {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return Black;
            // Compute row
            var rowOffset = (TileIndex * 16) + (y * 2); // 2 bytes per row
            // Fetch row parts
            var firstByte = VramReference[rowOffset];
            var secondByte = VramReference[rowOffset + 1];
            // Combine correct bits (first byte is low, 2nd high)
            var mask = 0b1000_0000 >> x;
            return (byte)(((firstByte & mask) != 0 ? 0b01 : 0b00) |  ((secondByte & mask) != 0 ? 0b10 : 0b00));
        }
    }

    public Bitmap ToBitmap(bool flipX = false, bool flipY = false) {
        Bitmap bmp = new Bitmap(Width, Height);
        for (var row = 0; row < Height; row++) {
            for (var col = 0; col < Width; col++) {
                var tCol = flipX ? (Width - 1) - col : col;
                var tRow = flipY ? (Height - 1) - row : row;
                bmp[col, row] = this[tCol, tRow] switch {
                    0 => ColourPallet.BackgroundWhite,
                    1 => ColourPallet.BackgroundLight,
                    2 => ColourPallet.BackgroundMedium,
                    3 => ColourPallet.BackgroundDark,
                    _ => ColourPallet.BackgroundDark
                };
            }
        }
        return bmp; 
    }
}