using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Players;

/// <summary>
/// Character set used for the Cli Renderer
/// </summary>
public class CliRendererCharacterSet {
    /// <summary>
    /// Character used for full darkness
    /// </summary>
    public char DarkCharacter {get; private set;}
    /// <summary>
    /// Character used for medium darkness
    /// </summary>
    public char MediumCharacter {get; private set;} 
    /// <summary>
    /// Character used for light shadows
    /// </summary>
    public char LightCharacter  {get; private set;}
     /// <summary>
    /// Character used for bright areas
    /// </summary>
    public char WhiteCharacter {get; private set;}

    /// <summary>
    /// Create a new character set for CLI rendering
    /// </summary>
    /// <param name="white">bright areas</param>
    /// <param name="light">shadow areas</param>
    /// <param name="medium">medium dark areas</param>
    /// <param name="dark">full dark areas</param>
    public CliRendererCharacterSet(char white, char light, char medium, char dark) {
        this.DarkCharacter = dark;
        this.MediumCharacter = medium;
        this.LightCharacter = light;
        this.WhiteCharacter = white;
    }

    /// <summary>
    /// Character set using only standard ASCII characters
    /// </summary>
    public static readonly CliRendererCharacterSet Ascii = new CliRendererCharacterSet(' ', '-', 'B', '@');
    /// <summary>
    /// Character set using Braille ASCII
    /// </summary>
    public static readonly CliRendererCharacterSet Braille = new CliRendererCharacterSet(' ', '⢁', '⠳', '⣿');
}

/// <summary>
/// Render generated bitmaps to the console
/// </summary>
public class CliRenderer {

    /// <summary>
    /// Character set used for renderering
    /// </summary>
    public CliRendererCharacterSet CharacterSet {get; set;}

    /// <summary>
    /// Create a new renderer with the given characters
    /// </summary>
    /// <param name="characters">drawing character set</param>
    public CliRenderer(CliRendererCharacterSet characters) {
        this.CharacterSet = characters;
    }

    /// <summary>
    /// Draw the bitmap to the console
    /// </summary>
    /// <param name="bmp">bitmap to draw</param>
    public void ToConsole(Bitmap bmp) {
        for (var row = 0; row < Gpu.LCD_HEIGHT; row++) {
            for (var col = 0; col < Gpu.LCD_WIDTH; col++) {
                switch (bmp[col, row]) {
                    case ColourPallet.BackgroundDark:
                    case ColourPallet.Object0Dark:
                    case ColourPallet.Object1Dark:
                        Console.Write(CharacterSet.DarkCharacter);
                        break;
                    case ColourPallet.BackgroundMedium:
                    case ColourPallet.Object0Medium:
                    case ColourPallet.Object1Medium:
                        Console.Write(CharacterSet.MediumCharacter);
                        break;
                    case ColourPallet.BackgroundLight:
                    case ColourPallet.Object0Light:
                    case ColourPallet.Object1Light:
                        Console.Write(CharacterSet.LightCharacter);
                        break;
                    case ColourPallet.BackgroundWhite:
                    case ColourPallet.Object0White:
                    case ColourPallet.Object1White:
                        Console.Write(CharacterSet.WhiteCharacter);
                        break;
                }
            }
            Console.WriteLine();
        }
    }
}