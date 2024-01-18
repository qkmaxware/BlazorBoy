namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// Wrapper for sprite data extracted from the OAM
/// </summary>
public class SpriteSpan {
    /// <summary>
    /// Sprite ID number
    /// </summary>
    public int Id {get; private set;}
    /// <summary>
    /// Byte offset into the OAM for the sprite data
    /// </summary>
    public int ByteOffset {get; private set;}
    /// <summary>
    /// Reference to the OAM data where the sprite exists
    /// </summary>
    private byte[] OamReference;

    /// <summary>
    /// Y coordinate of the sprite relative to the bottom of a LONG 1x2 sprite
    /// </summary>
    public byte YBottom {
        get => OamReference[ByteOffset + 0];
        set => OamReference[ByteOffset + 0] = value;
    }

    /// <summary>
    /// Y coordinate of the sprite relative to the top of the sprite
    /// </summary>
    public byte YTop {
        get => (byte)((int)OamReference[ByteOffset + 0] - 16);
        set => OamReference[ByteOffset + 0] = (byte)(value + 16);
    }

    /// <summary>
    /// X coordinate of the sprite relative to the right edge of the sprite
    /// </summary>
    public byte XRight {
        get => OamReference[ByteOffset + 1];
        set => OamReference[ByteOffset + 1] = value;
    }

    /// <summary>
    /// X coordinate of the sprite relative to the left edge of the sprite
    /// </summary>
    public byte XLeft {
        get => (byte)((int)OamReference[ByteOffset + 1] - 8);
        set => OamReference[ByteOffset + 1] = (byte)(value + 8);
    }
    
    /// <summary>
    /// Tile number to use for the sprite
    /// </summary>
    public byte TileNumber {
        get => OamReference[ByteOffset + 2];
        set => OamReference[ByteOffset + 2] = value;
    }

    /// <summary>
    /// Special property flags
    /// </summary>
    public byte Flags {
        get => OamReference[ByteOffset + 3];
        set => OamReference[ByteOffset + 3] = value;
    }

    public enum DrawPriority: byte {
        AboveBackground = (0), BelowBackground = (1)
    }
    public enum XOrientation : byte {
        Normal=(0), Flipped=(1)
    }
    public enum YOrientation: byte {
        Normal=(0), Flipped=(1)
    }
    public enum PaletteIndex : byte {
        Zero=(0), One=(1)
    }

    /// <summary>
    /// Pixel mixing priority
    /// </summary>
    public DrawPriority Priority => (Flags & 0b1000_0000) != 0 ? DrawPriority.BelowBackground : DrawPriority.AboveBackground;
    /// <summary>
    /// Y orientation
    /// </summary>
    public YOrientation FlipY => (Flags & 0b0100_0000) != 0 ? YOrientation.Flipped : YOrientation.Normal;
    /// <summary>
    /// X orientation
    /// </summary>
    public XOrientation FlipX => (Flags & 0b0010_0000) != 0 ? XOrientation.Flipped : XOrientation.Normal;
    /// <summary>
    /// Selected colour palette
    /// </summary>
    public PaletteIndex PaletteNumber => (Flags & 0b0001_0000) != 0 ? PaletteIndex.One : PaletteIndex.Zero;
    
    public SpriteSpan(int id, int offset, byte[] oam) {
        this.Id = id;
        this.ByteOffset = offset;
        this.OamReference = oam;
    }
}