using Godot;
using System;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class FpsCounter : Label {
	[Export] public TextureRenderer Renderer {get; set;}
	private int fps;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (Renderer is not null && Renderer.IsPlaying) {
			var fps = (int)Math.Round(Godot.Engine.GetFramesPerSecond());
			if (this.fps != fps) {
				// New string only if the FPS changed
				this.Text = "FPS: " + fps;
				this.fps = fps;
			}
		} else {
			this.Text = string.Empty;
		}
	}
}
