namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// Different types of sprite sizes
/// </summary>
public enum SpriteSize {
    Short1x1 = 0, Long1x2 = 1
}

/// <summary>
/// Tile map location indices
/// </summary>
public enum TileMapIndex {
    Map9800 = 0, Map9C00 = 1
}

/// <summary>
/// Tile map selection mode
/// </summary>
public enum TileDataSelect {
    Method8800 = 0, Method8000 = 1
}

/// <summary>
/// The LCDC register is one of the most important control registers for the LCD. Each of the 8 bits in this register is a flag which determines which elements are displayed and more.
/// </summary>
public struct LcdControlRegister {
    /// <summary>
    /// Underlying register value
    /// </summary>
    public int Value {get; set;}
    
    /// <summary>
    /// Setting this bit to 0 disables the PPU entirely. The screen is turned off.
    /// </summary>
    public bool IsLcdEnabled => (Value & 0b10000000) != 0;
    /// <summary>
    /// If set to 1, the Window will use the background map located at $9C00-$9FFF. Otherwise, it uses $9800-$9BFF.
    /// </summary>
    public TileMapIndex WindowTileMapIndex => (Value & 0b01000000) != 0 ? TileMapIndex.Map9C00 : TileMapIndex.Map9800;
    /// <summary>
    /// Setting this bit to 0 hides the window layer entirely.
    /// </summary>
    public bool IsWindowEnabled => (Value & 0b00100000) != 0;
    /// <summary>
    /// If set to 1, fetching Tile Data uses the 8000 method. Otherwise, the 8800 method is used.
    /// </summary>
    public TileDataSelect TileDataSelect => (Value & 0b00010000) != 0 ? TileDataSelect.Method8000 : TileDataSelect.Method8800;
    /// <summary>
    /// If set to 1, the Background will use the background map located at $9C00-$9FFF. Otherwise, it uses $9800-$9BFF.
    /// </summary>
    public TileMapIndex BackgroundTileMapIndex => (Value & 0b00001000) != 0 ? TileMapIndex.Map9C00 : TileMapIndex.Map9800;
    /// <summary>
    /// If set to 1, sprites are displayed as 1x2 Tile (8x16 pixel) object. Otherwise, they're 1x1 Tile.
    /// </summary>
    public SpriteSize SpriteSize => (Value & 0b00000100) != 0 ? SpriteSize.Long1x2 : SpriteSize.Short1x1;
    /// <summary>
    /// Sprites are only drawn to screen if this bit is set to 1.
    /// </summary>
    public bool IsSpritesEnabled => (Value & 0b00000010) != 0 ;
    /// <summary>
    /// If this bit is set to 0, neither Background nor Window tiles are drawn. Sprites are unaffected.
    /// </summary>
    public bool IsBackgroundEnabled => (Value & 0b00000001) != 0 ;
}