using Qkmaxware.Emulators.Gameboy.Hardware;
using System.Runtime.InteropServices;

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
    /// Character set using only simple ASCII characters
    /// </summary>
    public static readonly CliRendererCharacterSet Ascii = new CliRendererCharacterSet(' ', '-', 'X', '@');
    /// <summary>
    /// Character set using only standard Density characters
    /// </summary>
    public static readonly CliRendererCharacterSet Density = new CliRendererCharacterSet(' ', '░', '▒', '▓');
    /// <summary>
    /// Character set using Braille characters
    /// </summary>
    public static readonly CliRendererCharacterSet Braille = new CliRendererCharacterSet(' ', '⢁', '⠳', '⣿');
}

/// <summary>
/// Render generated bitmaps to the console
/// </summary>
public class CliRenderer {

    [StructLayout(LayoutKind.Sequential)]
    struct Coord {
        public short x, y;
        public Coord(short x, short y) {
            this.x = x; this.y = y;
        }
    };
    [StructLayout(LayoutKind.Explicit)]
    struct CharInfo {
        [FieldOffset(0)] public ushort Char;
        [FieldOffset(2)] public short Attributes;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct Rectangle {
        public short left, top, right, bottom;
        public Rectangle(short left, short top, short right, short bottom) {
            this.left = left; this.top = top; this.right = right; this.bottom = bottom;
        }
    }

    /// <summary>
    /// Character set used for rendering
    /// </summary>
    public CliRendererCharacterSet CharacterSet {get; set;}

    /// <summary>
    /// Window x coordinate
    /// </summary>
    public short WindowX {get; private set;}
    /// <summary>
    /// Window y coordinate
    /// </summary>
    public short WindowY {get; private set;}

    /// <summary>
    /// Create a new renderer with the given characters
    /// </summary>
    /// <param name="characters">drawing character set</param>
    /// <param name="scale">scale of the renderer relative to normal dimensions</param>
    public CliRenderer(CliRendererCharacterSet characters, float scale = 1) {
        this.CharacterSet = characters;
        Console.Clear();
        Console.SetCursorPosition(0, 0);
        Console.CursorVisible = false;
        var position = Console.GetCursorPosition();
        this.WindowX = (short)position.Left;
        this.WindowY = (short)position.Top;
        this.scale = scale;
        this.charsWidth = (short)(Gpu.LCD_WIDTH  * scale);
        this.charsHeight = (short)(Gpu.LCD_HEIGHT * scale);
        chars = new CharInfo[charsHeight * charsWidth];
        changed = new bool[chars.Length];
        for (var i = 0; i < chars.Length; i++) {
            chars[i].Char = CharacterSet.DarkCharacter;
            chars[i].Attributes = colorset(Console.ForegroundColor, Console.BackgroundColor);
            changed[i] = true;
        }
        prepareBuffer();
    }

    private static short colorset(ConsoleColor foreground, ConsoleColor background)
        => (short)(foreground + ((short)background << 4));

    /// <summary>
    /// Draw the bitmap to the console
    /// </summary>
    /// <param name="bmp">bitmap to draw</param>
    public void ToConsole(Bitmap bmp) {
        ToConsoleAsConsoleWrite(bmp);      
    }

    private float scale;
    private short charsWidth;
    private short charsHeight;
    private CharInfo[] chars;
    private bool[] changed;
    private void clear() {
        for (int i = 0; i < chars.Length; i++) {
            chars[i].Char = CharacterSet.DarkCharacter;
        }
    }
    private char getChar(short x, short y) {
        var addr = charsWidth * y + x;
        if (addr < 0 || addr >= chars.Length)
            return '\0';
        return (char)chars[addr].Char;
    }
    private void setChar(short x, short y, char new_value) {
        var xOnWindow = (short)MathF.Round(x * scale);
        var yOnWindow = (short)MathF.Round(y * scale);
        var addr = charsWidth * yOnWindow + xOnWindow;
        if (addr < 0 || addr >= chars.Length)
            return;

        var current_value = chars[addr].Char;
        chars[addr].Char = new_value; // TODO value blend if multiple bmp pixels go same scaled pixel
        if (new_value != current_value) {
            changed[addr] = true;
        }
    }
    private void fillChars(Bitmap bmp) {
        // Fill arrays
        for (short row = 0; row < Gpu.LCD_HEIGHT; row++) {
            for (short col = 0; col < Gpu.LCD_WIDTH; col++) {
                switch (bmp[col, row]) {
                    case ColourPallet.BackgroundDark:
                    case ColourPallet.Object0Dark:
                    case ColourPallet.Object1Dark:
                        setChar(col, row, CharacterSet.DarkCharacter);
                        break;
                    case ColourPallet.BackgroundMedium:
                    case ColourPallet.Object0Medium:
                    case ColourPallet.Object1Medium:
                        setChar(col, row, CharacterSet.MediumCharacter);
                        break;
                    case ColourPallet.BackgroundLight:
                    case ColourPallet.Object0Light:
                    case ColourPallet.Object1Light:
                        setChar(col, row, CharacterSet.LightCharacter);
                        break;
                    case ColourPallet.BackgroundWhite:
                    case ColourPallet.Object0White:
                    case ColourPallet.Object1White:
                        setChar(col, row, CharacterSet.WhiteCharacter);
                        break;
                }
            }
        }
    }

    private void prepareBuffer() {
        for (short row = 0; row < charsHeight; row++) {
            if (row != 0)
                Console.WriteLine();
            for (short col = 0; col < charsWidth; col++) {
                Console.Write(CharacterSet.DarkCharacter);
            }
        }
    }

    /// <summary>
    /// Draw the bitmap to console using classic Console.Write and Console.WriteLine invocations
    /// </summary>
    /// <param name="bmp">bitmap to draw</param>
    private void ToConsoleAsConsoleWrite(Bitmap bmp) {
        fillChars(bmp);

        // Draw array
        for (short row = 0; row < charsHeight; row++) {
            for (short col = 0; col < charsWidth; col++) {
                var addr = charsWidth * row + col;
                if (changed[addr]) {
                    Console.SetCursorPosition(this.WindowX + col, this.WindowY + row);
                    Console.Write(getChar(col, row));
                    changed[addr] = false;
                }
            }
        }
    }
}