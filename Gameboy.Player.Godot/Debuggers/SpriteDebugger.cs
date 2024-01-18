using Godot;
using System;
using System.Linq;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class SpriteDebugger : Control {

	[Export] public TextureRenderer Player;

	private BitmapTextureRect[] spriteTextures;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		spriteTextures = new BitmapTextureRect[40];
		for (var i = 0; i < spriteTextures.Length; i++) {
			var texture = this.GetNode<BitmapTextureRect>("ScrollContainer/GridContainer/Sprite " + i);
			var label = texture.GetNode<Label>("Label");
			label.Text = i.ToString();
			spriteTextures[i] = texture;
		}
	}

	public void Refresh() {
		var ppu = Player?.Console?.GPU;
		if (ppu is IDebuggablePpu debug) {
			foreach (var pair in debug.Sprites.Zip(spriteTextures)) {
				var sprite = pair.First;
				var tile = debug.Tiles.ElementAtOrDefault(sprite.TileNumber);
				var texture = pair.Second;

				var image = tile.ToBitmap();
				if (image.Height != 8 || image.Width != 8) {
					GD.Print("Mismatched image dimensions");
				}
				texture.Redraw(
					tile.ToBitmap(
						flipX: sprite.FlipX == SpriteSpan.XOrientation.Flipped,
						flipY: sprite.FlipY == SpriteSpan.YOrientation.Flipped
					)
				);
			}
		}
	}
}
