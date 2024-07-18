using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class ThemePicker : OptionButton {
	
	[Export] public Screen Screen;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		foreach (var theme in LcdColorTheme.Named) {
			this.AddItem(theme.Name);
		}
	}

	public void SelectTheme(int index) {
		if (Screen is null)
			return;

		if (index >= 0 && index < LcdColorTheme.Named.Count) {
			var theme = LcdColorTheme.Named[index];
			Screen.BgBlack = theme.BgBlack;
			Screen.BgDarkGrey= theme.BgDarkGrey;
			Screen.BgLightGrey = theme.BgLightGrey;
			Screen.BgWhite = theme.BgWhite;

			Screen.Obj0Black 	= theme.Obj0Black;
			Screen.Obj0DarkGrey 	= theme.Obj0DarkGrey;
			Screen.Obj0LightGrey = theme.Obj0LightGrey;
			Screen.Obj0White 	= theme.Obj0White;

			Screen.Obj1Black 	= theme.Obj1Black;
			Screen.Obj1DarkGrey 	= theme.Obj1DarkGrey;
			Screen.Obj1LightGrey = theme.Obj1LightGrey;
			Screen.Obj1White 	= theme.Obj1White;
		}
	}
}
