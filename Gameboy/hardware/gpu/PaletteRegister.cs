namespace Qkmaxware.Emulators.Gameboy.Hardware;

public struct PaletteRegister {
    /// <summary>
    /// Underlying register value
    /// </summary>
    public int Value {get; set;}

    private ColourPallet Black;
    private ColourPallet DarkGrey;
    private ColourPallet LightGrey;
    private ColourPallet White;

    public PaletteRegister(ColourPallet black, ColourPallet dark, ColourPallet light, ColourPallet white) {
        this.Value = 0;
        this.Black = black;
        this.DarkGrey = dark;
        this.LightGrey = light;
        this.White = white;
    }

    public ColourPallet this[int index] {
        get {
            int v = (Value >> (index * 2)) & 3; //1,2,3
            return v switch {
                0 => White,
                1 => LightGrey,
                2 => DarkGrey,
                3 => Black,
                _ => Black,
            };
        } set {
            int iValue = value switch {
                ColourPallet.BackgroundWhite => 0,
                ColourPallet.Object0White => 0,
                ColourPallet.Object1White => 0,

                ColourPallet.BackgroundLight => 1,
                ColourPallet.Object0Light => 1,
                ColourPallet.Object1Light => 1,

                ColourPallet.BackgroundMedium => 2,
                ColourPallet.Object0Medium => 2,
                ColourPallet.Object1Medium => 2,

                ColourPallet.BackgroundDark => 3,
                ColourPallet.Object0Dark => 3,
                ColourPallet.Object1Dark => 3,
                _ => 3
            };
            var mask = 0b11 << (index * 2);
            var inverseMask = ~mask;
            this.Value &= inverseMask;
            this.Value |= iValue << (index * 2);
        }
    }
}