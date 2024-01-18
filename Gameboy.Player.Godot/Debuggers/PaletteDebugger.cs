using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class PaletteDebugger : Control {

	[Export] public TextureRenderer Player;

	private ColorRect[][] bg;
	private ColorRect[][] obj;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		bg = new ColorRect[1][];
		bg[0] = new ColorRect[4];
		for (var i = 0; i < 4; i++) {
			var b00 = Convert.ToString(i, 2).PadLeft(2, '0');
			bg[0][i] = this.GetNode<ColorRect>("ScrollContainer/HSplitContainer/Background/VBoxContainer/HBoxContainer/"+b00);
		}

		obj = new ColorRect[2][];
		obj[0] = new ColorRect[4];
		obj[1] = new ColorRect[4];
		for (var i = 0; i < 4; i++) {
			var b00 = Convert.ToString(i, 2).PadLeft(2, '0');
			obj[0][i] = this.GetNode<ColorRect>("ScrollContainer/HSplitContainer/Objects/VBoxContainer/HBoxContainer1/"+b00);
			obj[1][i] = this.GetNode<ColorRect>("ScrollContainer/HSplitContainer/Objects/VBoxContainer/HBoxContainer2/"+b00);
		}
	}

	public void Refresh() {
		var ppu = Player?.Console?.GPU;
		if (ppu is IDebuggablePpu debug) {
			HashSet<ColourPallet> bgcolours = new HashSet<ColourPallet>();
			foreach (var palette in debug.BackgroundPalettes.Zip(bg)) {
				foreach (var pair in palette.First.Zip(palette.Second)) {
					var palettedColour = pair.First;
					var rgb = palettedColour switch {
						ColourPallet.BackgroundDark => new Color(0,0,0,1),
						ColourPallet.Object0Dark => new Color(0,0,0,1),
						ColourPallet.Object1Dark => new Color(0,0,0,1),

						ColourPallet.BackgroundMedium => new Color(0.3f,0.3f,0.3f,1),
						ColourPallet.Object0Medium => new Color(0.3f,0.3f,0.3f,1),
						ColourPallet.Object1Medium => new Color(0.3f,0.3f,0.3f,1),

						ColourPallet.BackgroundLight => new Color(0.6f,0.6f,0.6f,1),
						ColourPallet.Object0Light => new Color(0.6f,0.6f,0.6f,1),
						ColourPallet.Object1Light => new Color(0.6f,0.6f,0.6f,1),

						ColourPallet.BackgroundWhite => new Color(1f,1f,1f,1),
						ColourPallet.Object0White => new Color(1f,1f,1f,1),
						ColourPallet.Object1White => new Color(1f,1f,1f,1),

						_ => new Color(0,0,0,1),
					};
					bgcolours.Add(palettedColour);
					pair.Second.Color = rgb;
				}
			}
			if (bgcolours.Count < 1) {
				GD.PushError("Background palette is mono-colour.");
			}

			HashSet<ColourPallet> objcolours = new HashSet<ColourPallet>();
			foreach (var palette in debug.ObjectPalettes.Zip(obj)) {
				foreach (var pair in palette.First.Zip(palette.Second)) {
					var palettedColour = pair.First;
					var rgb = palettedColour switch {
						ColourPallet.BackgroundDark => new Color(0,0,0,1),
						ColourPallet.Object0Dark => new Color(0,0,0,1),
						ColourPallet.Object1Dark => new Color(0,0,0,1),

						ColourPallet.BackgroundMedium => new Color(0.3f,0.3f,0.3f,1),
						ColourPallet.Object0Medium => new Color(0.3f,0.3f,0.3f,1),
						ColourPallet.Object1Medium => new Color(0.3f,0.3f,0.3f,1),

						ColourPallet.BackgroundLight => new Color(0.6f,0.6f,0.6f,1),
						ColourPallet.Object0Light => new Color(0.6f,0.6f,0.6f,1),
						ColourPallet.Object1Light => new Color(0.6f,0.6f,0.6f,1),

						ColourPallet.BackgroundWhite => new Color(1f,1f,1f,1),
						ColourPallet.Object0White => new Color(1f,1f,1f,1),
						ColourPallet.Object1White => new Color(1f,1f,1f,1),

						_ => new Color(0,0,0,1),
					};
					objcolours.Add(palettedColour);
					pair.Second.Color = rgb;
				}
			}
			if (objcolours.Count < 1) {
				GD.PushError("Object palette is mono-colour.");
			}
		}
	}
}
