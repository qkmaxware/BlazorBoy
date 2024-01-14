using static Qkmaxware.Emulators.Gameboy.Hardware.ColourPallet;
namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Bitmap {
    private byte[] pixels;

    public int Width {get; private set;}
    public int Height {get; private set;}

    public Bitmap(int width, int height) {
        this.pixels = new byte[height * width];
        this.Width = width; 
        this.Height = height;
    } 
    public Bitmap(ColourPallet[,] pixels) {
        this.Width = pixels.GetLength(1); 
        this.Height = pixels.GetLength(0);
        this.pixels = new byte[this.Width * this.Height];
        for (var y = 0; y < this.Height; y++) {
            for (var x = 0; x < this.Width; x++) {
                this[x, y] = pixels[y, x];
            }
        }
    } 

    public ColourPallet this[int x, int y] {
        get {
            if (!IsValidCoordinate(x, y)) {
                return ColourPallet.BackgroundDark;
            }
            return (ColourPallet)pixels[y * this.Width + x];
        }
        set {
            if (!IsValidCoordinate(x, y)) {
                return;
            }
            pixels[y * this.Width + x] = (byte)value;
        }
    }

    public void Fill(ColourPallet colour) {
        Array.Fill(this.pixels, (byte)colour);
    }

    public bool IsValidRow(int row) {
        return row >= 0 && row < this.Height;
    }

    public bool IsValidColumn(int column) {
        return column >= 0 && column < this.Width;
    }

    public bool IsValidCoordinate(int x, int y) {
        return IsValidColumn(x) && IsValidRow(y);
    }

    public void DrawRect(int xOrig, int yOrig, int width, int height, ColourPallet colour) {
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var cX = xOrig + x;
                var cY = yOrig + y;
                if (IsValidCoordinate(cX, cY)) {
                    this[cX, cY] = colour;
                }
            }
        }
    }

    public void Stamp(int xOrig, int yOrig, Bitmap stamp) {
        for (var y = 0; y < stamp.Height; y++) {
            for (var x = 0; x < stamp.Width; x++) {
                var cX = xOrig + x;
                var cY = yOrig + y;
                if (IsValidCoordinate(cX, cY)) {
                    this[cX, cY] = stamp[x, y];
                }
            }
        }
    }
    
    public Bitmap Enlarge (int times) {
        times = Math.Max(1, times);
        Bitmap next = new Bitmap(this.Width * times, this.Height * times);
        for (var y = 0; y < this.Height; y++) {
            for (var x = 0; x < this.Width; x++) {
                var colour = this[x, y];
                for (var xt = 0; xt < times; xt++) {
                    for (var yt = 0; yt < times; yt++) {
                        next[x * times + xt, y * times + yt] = colour;
                    }
                }
            }
        }
        return next;
    }

    public byte[] GetBytes() => this.pixels;

    public static readonly Bitmap StampB = new Bitmap(new ColourPallet[,]{
        { Dark, Dark, White },
        { Dark, White, Dark },
        { Dark, Dark, White },
        { Dark, White, Dark },
        { Dark, Dark, Dark }
    });

    public static readonly Bitmap StampL = new Bitmap(new ColourPallet[,]{
        { Dark, White, White },
        { Dark, White, White },
        { Dark, White, White },
        { Dark, White, White },
        { Dark, Dark, Dark }
    });

    public static readonly Bitmap StampA = new Bitmap(new ColourPallet[,]{
        { Dark, Dark, Dark },
        { Dark, White, Dark },
        { Dark, Dark, Dark },
        { Dark, White, Dark },
        { Dark, White, Dark }
    });

    public static readonly Bitmap StampZ = new Bitmap(new ColourPallet[,]{
        { Dark, Dark, Dark },
        { White, White, Dark },
        { White, Dark, White },
        { Dark, White, White },
        { Dark, Dark, Dark }
    });

    public static readonly Bitmap StampO = new Bitmap(new ColourPallet[,]{
        { Dark, Dark, Dark },
        { Dark, White, Dark },
        { Dark, White, Dark },
        { Dark, White, Dark },
        { Dark, Dark, Dark }
    });

    public static readonly Bitmap StampR = new Bitmap(new ColourPallet[,]{
        { Dark, Dark, White },
        { Dark, White, Dark },
        { Dark, Dark, White },
        { Dark, White, Dark },
        { Dark, White, Dark }
    });

    public static readonly Bitmap StampY = new Bitmap(new ColourPallet[,]{
        { Dark, White, Dark },
        { Dark, White, Dark },
        { White, Dark, White },
        { White, Dark, White },
        { White, Dark, White }
    });
}