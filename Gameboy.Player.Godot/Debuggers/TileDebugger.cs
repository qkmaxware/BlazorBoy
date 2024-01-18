using Godot;
using System;
using System.Linq;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class TileDebugger : Control {

	[Export] public TextureRenderer Player;

	private HFlowContainer container;
	[Export]
	public PackedScene TileTemplate;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		container = GetNode<HFlowContainer>("ScrollContainer/HFlowContainer");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public void Refresh() {
		foreach (Node node in container.GetChildren()) {
			container.RemoveChild(node);
			node.QueueFree();
		}
		
		var ppu = Player?.Console?.GPU;
		if (ppu is IDebuggablePpu debug) {
			foreach (var tile in debug.Tiles) {
				var spawned = TileTemplate.Instantiate<BitmapTextureRect>();
				spawned.Visible = true;
				spawned.Redraw(tile.ToBitmap());
				container.AddChild(spawned);
			}
		}
	}
}
