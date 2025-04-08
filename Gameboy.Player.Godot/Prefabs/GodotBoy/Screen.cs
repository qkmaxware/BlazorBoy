using Godot;
using System;
using System.Linq;
using Qkmaxware.Emulators.Gameboy;
using Qkmaxware.Emulators.Gameboy.Hardware;
using LcdBitmap = Qkmaxware.Emulators.Gameboy.Hardware.Bitmap;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class Screen : TabContainer {

	[Export]
	[ExportGroup("Colour Pallet/Background")]
	public Color BgWhite = new Color(1, 1, 1);
	[Export]
	public Color BgLightGrey = new Color(0.6f, 0.6f, 0.6f);
	[Export]
	public Color BgDarkGrey = new Color(0.3f, 0.3f, 0.3f);
	[Export]
	public Color BgBlack = new Color(0, 0, 0);

	[Export]
	[ExportGroup("Colour Pallet/Object 0")]
	public Color Obj0White = new Color(1, 1, 1);
	[Export]
	public Color Obj0LightGrey = new Color(0.6f, 0.6f, 0.6f);
	[Export]
	public Color Obj0DarkGrey = new Color(0.3f, 0.3f, 0.3f);
	[Export]
	public Color Obj0Black = new Color(0, 0, 0);

	[Export]
	[ExportGroup("Colour Pallet/Object 1")]
	public Color Obj1White = new Color(1, 1, 1);
	[Export]
	public Color Obj1LightGrey = new Color(0.6f, 0.6f, 0.6f);
	[Export]
	public Color Obj1DarkGrey = new Color(0.3f, 0.3f, 0.3f);
	[Export]
	public Color Obj1Black = new Color(0, 0, 0);

	private LcdBitmap intro;
	private LcdBitmap blank; 

	private Image image;
	private ImageTexture texture;
	private TextureRect[] screens;

    public override void _Ready() {
        base._Ready();
		var tabs = this.GetTabCount();
		screens = new TextureRect[tabs];
		for (int i = 0; i < tabs; i++) {
			screens[i] = GetTabControl(i).GetNode<TextureRect>("Screen");
		}

		var intro = new LcdBitmap(Ppu.LCD_WIDTH, Ppu.LCD_HEIGHT);
		this.intro = intro;
		intro.Fill(ColourPallet.BackgroundDark);

		blank = new LcdBitmap(Ppu.LCD_WIDTH, Ppu.LCD_HEIGHT);
		blank.Fill(ColourPallet.BackgroundDark);

		var text = new LcdBitmap[]{ LcdBitmap.StampB, LcdBitmap.StampL, LcdBitmap.StampA, LcdBitmap.StampZ, LcdBitmap.StampO, LcdBitmap.StampR, LcdBitmap.StampB, LcdBitmap.StampO, LcdBitmap.StampY }.Select(stamp => stamp.Invert().Enlarge(4)).ToArray();
		var width = text.Select(stamp => stamp.Width + 1).Sum();
		var height = text.Select(stamp => stamp.Height).Max();

		var startX = (intro.Width / 2) - (width / 2);
		var startY = (intro.Height / 2) - (height / 2);
		foreach (var stamp in text) {
			intro.Stamp(startX, startY, stamp);
			startX += stamp.Width + 1;
		}

		this.Redraw(this.intro, assignToAllScreens: true);
    }

	public void ShowIntro() {
		this.Redraw(intro);
	}

	public void Blank() {
		this.Redraw(blank);
	}

    public void Redraw(LcdBitmap bmp, bool assignToAllScreens = false) {
		if (this.image is null) {
			this.image = Image.Create(width: bmp.Width, height: bmp.Height, useMipmaps: false, format: Image.Format.Rgb8);
		}
		var pixels = this.image;
	
		for (var col = 0; col < bmp.Height; col++) {
			for (var row = 0; row < bmp.Width; row++) {
				pixels.SetPixel(row, col, bmp[row, col] switch {
					ColourPallet.BackgroundDark => BgBlack,
					ColourPallet.Object0Dark => Obj0Black,
					ColourPallet.Object1Dark => Obj1Black,

					ColourPallet.BackgroundMedium => BgDarkGrey,
					ColourPallet.Object0Medium => Obj0DarkGrey,
					ColourPallet.Object1Medium => Obj1DarkGrey,

					ColourPallet.BackgroundLight => BgLightGrey,
					ColourPallet.Object0Light => Obj0LightGrey,
					ColourPallet.Object1Light => Obj1LightGrey,

					ColourPallet.BackgroundWhite => BgWhite,
					ColourPallet.Object0White => Obj0White,
					ColourPallet.Object1White => Obj1White,

					_ => BgBlack,
				});
			}
		}
		
		if (texture is null) {
			texture = ImageTexture.CreateFromImage(pixels);
		} else {
			texture.Update(pixels);
		}
		if (assignToAllScreens) {
			foreach (var screen in this.screens) {
				if (screen is null)
					continue;
				screen.Texture = texture;
			}
		} else {
			var screen = screens[CurrentTab];
			if (screen is not null)
				screen.Texture = texture;
		}
		
	}
}
