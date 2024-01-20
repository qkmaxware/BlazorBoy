namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// Picture processing unit interface with additional debug methods
/// </summary>
public interface IDebuggablePpu : IPpu {
    public IEnumerable<ColourPallet[]> BackgroundPalettes {get;}
    public IEnumerable<ColourPallet[]> ObjectPalettes {get;} 
    public IEnumerable<SpriteSpan> Sprites {get;}
    public IEnumerable<TileSpan> Tiles {get;}
    public TileDataSelect TileSelectionMode {get;}
    public BackgroundMapSpan BackgroundMap {get;}
    public BackgroundMapSpan WindowMap {get;}
}