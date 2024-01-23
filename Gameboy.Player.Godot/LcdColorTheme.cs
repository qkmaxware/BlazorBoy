using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;

public class LcdColorTheme {
	public string Name;
	public Color BgWhite = new Color(1, 1, 1);	
	public Color BgLightGrey = new Color(0.6f, 0.6f, 0.6f);
	public Color BgDarkGrey = new Color(0.3f, 0.3f, 0.3f);
	public Color BgBlack = new Color(0, 0, 0);

	public Color Obj0White = new Color(1, 1, 1);
	public Color Obj0LightGrey = new Color(0.6f, 0.6f, 0.6f);
	public Color Obj0DarkGrey = new Color(0.3f, 0.3f, 0.3f);
	public Color Obj0Black = new Color(0, 0, 0);

	public Color Obj1White = new Color(1, 1, 1);
	public Color Obj1LightGrey = new Color(0.6f, 0.6f, 0.6f);
	public Color Obj1DarkGrey = new Color(0.3f, 0.3f, 0.3f);
	public Color Obj1Black = new Color(0, 0, 0);


    public static readonly List<LcdColorTheme> Named = new List<LcdColorTheme> {
		// Good theme list https://tcrf.net/Notes:Game_Boy_Color_Bootstrap_ROM
		new LcdColorTheme { Name = "Greyscale" },
		new LcdColorTheme { 
			Name = "Classic Green",
			BgWhite = new Color("e0f8cf"),	
			BgLightGrey = new Color("86c06c"),
			BgDarkGrey = new Color("306850"),
			BgBlack = new Color("071821"),
			Obj0White = new Color("e0f8cf"),
			Obj0LightGrey = new Color("86c06c"),
			Obj0DarkGrey = new Color("306850"),
			Obj0Black = new Color("071821"),
			Obj1White = new Color("e0f8cf"),
			Obj1LightGrey = new Color("86c06c"),
			Obj1DarkGrey = new Color("306850"),
			Obj1Black = new Color("071821"),
		},
		new LcdColorTheme { 
			Name = "PkMn Blue",
			BgWhite = new Color("FFFFFF"),	
			Obj0White = new Color("FFFFFF"),
			Obj1White = new Color("FFFFFF"),

			BgLightGrey = new Color("63A5FF"),
			Obj0LightGrey = new Color("FF8484"),
			Obj1LightGrey = new Color("63A5FF"),

			BgDarkGrey = new Color("0000FF"),
			Obj0DarkGrey = new Color("943A3A"),
			Obj1DarkGrey = new Color("0000FF"),

			BgBlack = new Color("000000"),
			Obj0Black = new Color("000000"),
			Obj1Black = new Color("000000"),
		},
		new LcdColorTheme { 
			Name = "PkMn Red",
			BgWhite = new Color("FFFFFF"),	
			Obj0White = new Color("FFFFFF"),
			Obj1White = new Color("FFFFFF"),

			BgLightGrey = new Color("FF8484"),
			Obj0LightGrey = new Color("7BFF31"),
			Obj1LightGrey = new Color("FF8484"),

			BgDarkGrey = new Color("943A3A"),
			Obj0DarkGrey = new Color("008400"),
			Obj1DarkGrey = new Color("943A3A"),

			BgBlack = new Color("000000"),
			Obj0Black = new Color("000000"),
			Obj1Black = new Color("000000"),
		},
		new LcdColorTheme { 
			Name = "Link's Awakening",
			BgWhite = new Color("FFFFFF"),	
			Obj0White = new Color("FFFFFF"),
			Obj1White = new Color("FFFFFF"),

			BgLightGrey = new Color("FF8484"),
			Obj0LightGrey = new Color("00FF00"),
			Obj1LightGrey = new Color("63A5FF"),

			BgDarkGrey = new Color("943A3A"),
			Obj0DarkGrey = new Color("008400"),
			Obj1DarkGrey = new Color("0000FF"),

			BgBlack = new Color("000000"),
			Obj0Black = new Color("004A00"),
			Obj1Black = new Color("000000"),
		},
	};
}