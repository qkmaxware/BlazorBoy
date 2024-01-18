namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// Different modes for the picture processing unit
/// </summary>
public enum PpuMode {
    HBlank = 0,
    VBlank = 1,
    OamScan = 2,
    Drawing = 3
}

/// <summary>
/// The STAT register contains both information-bits which allow the CPU to determine the status of the PPU, as well as bits which affect the interrupt trigger behavior of the PPU.
/// </summary>
public struct LcdStatusRegister {
    /// <summary>
    /// Underlying register value
    /// </summary>
    public int Value {get; set;}

    /// <summary>
    /// Setting this bit to 1 enables the "LYC=LY condition" to trigger a STAT interrupt.
    /// </summary>
    public bool IsLycLyInterruptEnabled => (Value & 0b01000000) != 0;
    /// <summary>
    /// Setting this bit to 1 enables the "mode 2 condition" to trigger a STAT interrupt.
    /// </summary>
    public bool IsOamScanInterruptEnabled => (Value & 0b00100000) != 0;
    /// <summary>
    /// Setting this bit to 1 enables the "mode 1 condition" to trigger a STAT interrupt.
    /// </summary>
    public bool IsVBlankInterruptEnabled => (Value & 0b00010000) != 0;
    /// <summary>
    /// Setting this bit to 1 enables the "mode 0 condition" to trigger a STAT interrupt.
    /// </summary>
    public bool IsHBlankInterruptEnabled => (Value & 0b00001000) != 0;
    /// <summary>
    /// This bit is set by the PPU if the value of the LY register is equal to that of the LYC register.
    /// </summary>
    public bool IsLycCoincidentallyLy {
        get => (Value & 0b0000_0100) != 0;
        set {
            var cleared = Value & 0b1111_1011;
            if (value)
                cleared |= 0b0000_0100;
        }
    }
    /// <summary>
    /// These two bits are set by the PPU depending on which mode it is in.
    /// </summary>
    public PpuMode Mode {
        get => (Value & 0b00000011) switch {
                0 => PpuMode.HBlank,
                1 => PpuMode.VBlank,
                2 => PpuMode.OamScan,
                3 => PpuMode.Drawing,
                _ => PpuMode.Drawing
            };
        set {
            var ivalue = (int)value;
            this.Value &= 0b1111_1100;  // Clear last 2 bits
            this.Value |= ivalue;       // Set last 2 bits
        }
    }
}