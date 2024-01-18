using Godot;
using System;
using Qkmaxware.Emulators.Gameboy;
using Qkmaxware.Emulators.Gameboy.Hardware;
using LcdBitmap = Qkmaxware.Emulators.Gameboy.Hardware.Bitmap;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class BitmapTextureRect : TextureRect {

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

	private ImageTexture texture;

    public void Redraw(LcdBitmap bmp) {
		var pixels = Image.Create(width: bmp.Width, height: bmp.Height, useMipmaps: false, format: Image.Format.Rgb8);
	
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
		this.Texture = texture;
	}
}