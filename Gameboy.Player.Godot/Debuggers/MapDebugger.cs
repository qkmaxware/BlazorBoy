using Godot;
using System;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public enum MapType {
	Background, Window
}

public partial class MapDebugger : Control {

	[Export] public TextureRenderer Player;
	[Export] public MapType Map; 

	private BitmapTextureRect rect;
	private Label label;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.rect = GetNode<BitmapTextureRect>("Rect");
		this.label = GetNode<Label>("Label");
	}

	public void Refresh() {
		var ppu = Player?.Console?.GPU;
		if (ppu is IDebuggablePpu debug) {
			var mode = debug.TileSelectionMode;
			label.Text = "Addressing Mode: " + mode;
			switch (Map) {
				case MapType.Background:
					rect?.Redraw(debug.BackgroundMap.ToBitmap(mode));
					break;
				case MapType.Window:
					rect?.Redraw(debug.WindowMap.ToBitmap(mode));
					break;
			}
		}
	}
}
