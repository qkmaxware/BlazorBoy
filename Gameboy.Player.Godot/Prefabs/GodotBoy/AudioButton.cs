using Godot;
using System;

public partial class AudioButton : Button {

	[Export] public Speakers Speakers;

	[ExportGroup("Icons")]
	[Export] public Texture2D AudioPlayingIcon;
	[Export] public Texture2D AudioPausedIcon;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.Icon = AudioPausedIcon;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (Speakers is null) {
			return;
		}

		if (Speakers.EnableSound) {
			if (this.Icon != AudioPlayingIcon)
				this.Icon = AudioPlayingIcon;
		} else {
			if (this.Icon != AudioPausedIcon)
				this.Icon = AudioPausedIcon;
		}
	}
}
