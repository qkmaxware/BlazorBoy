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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.rect = GetNode<BitmapTextureRect>("Rect");
	}

	public void Refresh() {
		var ppu = Player?.Console?.GPU;
		if (ppu is IDebuggablePpu debug) {
			switch (Map) {
				case MapType.Background:
					rect?.Redraw(debug.BackgroundMap.ToBitmap());
					break;
				case MapType.Window:
					rect?.Redraw(debug.WindowMap.ToBitmap());
					break;
			}
		}
	}
}
