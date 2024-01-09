using System.Drawing;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public enum ColourPallet : byte {
    Dark = 0, Medium = 1, Light = 2, White = 3,
    BackgroundDark = 0, BackgroundMedium = 1, BackgroundLight = 2, BackgroundWhite = 3,
    Object0Dark = 4, Object0Medium = 5, Object0Light = 6, Object0White = 7,
    Object1Dark = 8, Object1Medium = 9, Object1Light = 10, Object1White = 11,
}