using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class PaletteDebugger : Control {

	[Export] public GodotBoy Player;

	private ColorPickerButton bg0Black;
	private ColorPickerButton bg0DarkGrey;
	private ColorPickerButton bg0LightGrey;
	private ColorPickerButton bg0White;
	private ColorPickerButton obj0Black;
	private ColorPickerButton obj0DarkGrey;
	private ColorPickerButton obj0LightGrey;
	private ColorPickerButton obj0White;
	private ColorPickerButton obj1Black;
	private ColorPickerButton obj1DarkGrey;
	private ColorPickerButton obj1LightGrey;
	private ColorPickerButton obj1White;

	private ColorRect[][] bg;
	private ColorRect[][] obj;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Colours
		bg0Black = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Bg/HBoxContainer/BgBlack");
		bg0Black.Color = Player?.Screen?.BgBlack ?? new Color(0,0,0,1);
		bg0DarkGrey = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Bg/HBoxContainer/BgDarkGrey");
		bg0DarkGrey.Color = Player?.Screen?.BgDarkGrey ?? new Color(0.3f,0.3f,0.3f,1);
		bg0LightGrey = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Bg/HBoxContainer/BgLightGrey");
		bg0LightGrey.Color = Player?.Screen?.BgLightGrey ?? new Color(0.6f,0.6f,0.6f,1);
		bg0White = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Bg/HBoxContainer/BgWhite");
		bg0White.Color = Player?.Screen?.BgWhite ?? new Color(1,1,1,1);

		obj0Black = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer/Obj0Black");
		obj0Black.Color = Player?.Screen?.Obj0Black ?? new Color(0,0,0,1);
		obj0DarkGrey = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer/Obj0DarkGrey");
		obj0DarkGrey.Color = Player?.Screen?.Obj0DarkGrey ?? new Color(0.3f,0.3f,0.3f,1);
		obj0LightGrey = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer/Obj0LightGrey");
		obj0LightGrey.Color = Player?.Screen?.Obj0LightGrey ?? new Color(0.6f,0.6f,0.6f,1);
		obj0White = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer/Obj0White");
		obj0White.Color = Player?.Screen?.Obj0White ?? new Color(1,1,1,1);

		obj1Black = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer2/Obj1Black");
		obj1Black.Color = Player?.Screen?.Obj1Black ?? new Color(0,0,0,1);
		obj1DarkGrey = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer2/Obj1DarkGrey");
		obj1DarkGrey.Color = Player?.Screen?.Obj1DarkGrey ?? new Color(0.3f,0.3f,0.3f,1);
		obj1LightGrey = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer2/Obj1LightGrey");
		obj1LightGrey.Color = Player?.Screen?.Obj1LightGrey ?? new Color(0.6f,0.6f,0.6f,1);
		obj1White = GetNode<ColorPickerButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Objects/HBoxContainer2/Obj1White");
		obj1White.Color = Player?.Screen?.Obj1White ?? new Color(1,1,1,1);

		var themePicker = GetNode<OptionButton>("ScrollContainer/VSplitContainer/Colours/VBoxContainer/HBoxContainer/Themes/OptionButton");
		foreach (var theme in LcdColorTheme.Named) {
			themePicker.AddItem(theme.Name);
		}

		// Palettes
		bg = new ColorRect[1][];
		bg[0] = new ColorRect[4];
		for (var i = 0; i < 4; i++) {
			var b00 = Convert.ToString(i, 2).PadLeft(2, '0');
			bg[0][i] = this.GetNode<ColorRect>("ScrollContainer/VSplitContainer/Palettes/VBoxContainer/HBoxContainer/Background/HBoxContainer/"+b00);
		}

		obj = new ColorRect[2][];
		obj[0] = new ColorRect[4];
		obj[1] = new ColorRect[4];
		for (var i = 0; i < 4; i++) {
			var b00 = Convert.ToString(i, 2).PadLeft(2, '0');
			obj[0][i] = this.GetNode<ColorRect>("ScrollContainer/VSplitContainer/Palettes/VBoxContainer/HBoxContainer/Objects/HBoxContainer1/"+b00);
			obj[1][i] = this.GetNode<ColorRect>("ScrollContainer/VSplitContainer/Palettes/VBoxContainer/HBoxContainer/Objects/HBoxContainer2/"+b00);
		}
	}

	public void Refresh() {
		bg0Black.Color = Player?.Screen?.BgBlack ?? new Color(0,0,0,1);
		bg0DarkGrey.Color = Player?.Screen?.BgDarkGrey ?? new Color(0.3f,0.3f,0.3f,1);
		bg0LightGrey.Color = Player?.Screen?.BgLightGrey ?? new Color(0.6f,0.6f,0.6f,1);
		bg0White.Color = Player?.Screen?.BgWhite ?? new Color(1,1,1,1);

		obj0Black.Color = Player?.Screen?.Obj0Black ?? new Color(0,0,0,1);
		obj0DarkGrey.Color = Player?.Screen?.Obj0DarkGrey ?? new Color(0.3f,0.3f,0.3f,1);
		obj0LightGrey.Color = Player?.Screen?.Obj0LightGrey ?? new Color(0.6f,0.6f,0.6f,1);
		obj0White.Color = Player?.Screen?.Obj0White ?? new Color(1,1,1,1);

		obj1Black.Color = Player?.Screen?.Obj1Black ?? new Color(0,0,0,1);
		obj1DarkGrey.Color = Player?.Screen?.Obj1DarkGrey ?? new Color(0.3f,0.3f,0.3f,1);
		obj1LightGrey.Color = Player?.Screen?.Obj1LightGrey ?? new Color(0.6f,0.6f,0.6f,1);
		obj1White.Color = Player?.Screen?.Obj1White ?? new Color(1,1,1,1);

		var ppu = Player?.Console?.GPU;
		if (ppu is IDebuggablePpu debug) {
			HashSet<ColourPallet> bgcolours = new HashSet<ColourPallet>();
			foreach (var palette in debug.BackgroundPalettes.Zip(bg)) {
				foreach (var pair in palette.First.Zip(palette.Second)) {
					var palettedColour = pair.First;
					var rgb = palettedColour switch {
						ColourPallet.BackgroundDark => Player.Screen.BgBlack,
						ColourPallet.Object0Dark => Player.Screen.BgBlack,
						ColourPallet.Object1Dark => Player.Screen.BgBlack,

						ColourPallet.BackgroundMedium => Player.Screen.BgDarkGrey,
						ColourPallet.Object0Medium => Player.Screen.BgDarkGrey,
						ColourPallet.Object1Medium => Player.Screen.BgDarkGrey,

						ColourPallet.BackgroundLight => Player.Screen.BgLightGrey,
						ColourPallet.Object0Light => Player.Screen.BgLightGrey,
						ColourPallet.Object1Light => Player.Screen.BgLightGrey,

						ColourPallet.BackgroundWhite => Player.Screen.BgWhite,
						ColourPallet.Object0White => Player.Screen.BgWhite,
						ColourPallet.Object1White => Player.Screen.BgWhite,

						_ => Player.Screen.BgBlack,
					};
					bgcolours.Add(palettedColour);
					pair.Second.Color = rgb;
				}
			}
			if (bgcolours.Count < 1) {
				GD.PushError("Background palette is mono-colour.");
			}

			HashSet<ColourPallet> objcolours = new HashSet<ColourPallet>();
			var colourSetIndex = 0;
			foreach (var palette in debug.ObjectPalettes.Zip(obj)) {
				var black = colourSetIndex == 0 ? Player.Screen.Obj0Black : Player.Screen.Obj1Black;
				var dg = colourSetIndex == 0 ? Player.Screen.Obj0DarkGrey : Player.Screen.Obj1DarkGrey;
				var lg = colourSetIndex == 0 ? Player.Screen.Obj0LightGrey : Player.Screen.Obj1LightGrey;
				var white = colourSetIndex == 0 ? Player.Screen.Obj0White : Player.Screen.Obj1White;

				foreach (var pair in palette.First.Zip(palette.Second)) {
					var palettedColour = pair.First;
					var rgb = palettedColour switch {
						ColourPallet.BackgroundDark => black,
						ColourPallet.Object0Dark => black,
						ColourPallet.Object1Dark => black,

						ColourPallet.BackgroundMedium => dg,
						ColourPallet.Object0Medium => dg,
						ColourPallet.Object1Medium => dg,

						ColourPallet.BackgroundLight => lg,
						ColourPallet.Object0Light => lg,
						ColourPallet.Object1Light => lg,

						ColourPallet.BackgroundWhite => white,
						ColourPallet.Object0White => white,
						ColourPallet.Object1White => white,

						_ => black,
					};
					objcolours.Add(palettedColour);
					pair.Second.Color = rgb;
				}

				colourSetIndex = (colourSetIndex + 1) % 2;
			}
			if (objcolours.Count < 1) {
				GD.PushError("Object palette is mono-colour.");
			}
		}
	}


	public void SelectTheme(int index) {
		if (index >= 0 && index < LcdColorTheme.Named.Count) {
			var theme = LcdColorTheme.Named[index];
			bg0Black.Color = theme.BgBlack;
			bg0DarkGrey.Color = theme.BgDarkGrey;
			bg0LightGrey.Color = theme.BgLightGrey;
			bg0White.Color = theme.BgWhite;

			obj0Black.Color 	= theme.Obj0Black;
			obj0DarkGrey.Color 	= theme.Obj0DarkGrey;
			obj0LightGrey.Color = theme.Obj0LightGrey;
			obj0White.Color 	= theme.Obj0White;

			obj1Black.Color 	= theme.Obj1Black;
			obj1DarkGrey.Color 	= theme.Obj1DarkGrey;
			obj1LightGrey.Color = theme.Obj1LightGrey;
			obj1White.Color 	= theme.Obj1White;
		}
	}

	public void UpdatePlayerColours() {
		if (Player is not null) {
			Player.Screen.BgBlack = bg0Black.Color;
			Player.Screen.BgDarkGrey = bg0DarkGrey.Color;
			Player.Screen.BgLightGrey = bg0LightGrey.Color;
			Player.Screen.BgWhite = bg0White.Color;
			
			Player.Screen.Obj0Black = obj0Black.Color;
			Player.Screen.Obj0DarkGrey = obj0DarkGrey.Color;
			Player.Screen.Obj0LightGrey = obj0LightGrey.Color;
			Player.Screen.Obj0White = obj0White.Color;
			
			Player.Screen.Obj1Black = obj1Black.Color;
			Player.Screen.Obj1DarkGrey = obj1DarkGrey.Color;
			Player.Screen.Obj1LightGrey = obj1LightGrey.Color;
			Player.Screen.Obj1White = obj1White.Color;
		}
	}
}
