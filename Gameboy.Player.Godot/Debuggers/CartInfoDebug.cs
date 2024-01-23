using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Player;
public partial class CartInfoDebug : Control {

	[Export] public TextureRenderer Player;

	private RichTextLabel text;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		text = this.GetNode<RichTextLabel>("ScrollContainer/MarginContainer/RichTextLabel");
	}

	public void Refresh() {
		var str = string.Empty;

		if (Player?.Console?.GetCartridge() is Cartridge cart) {
			str = cart.Info.ToString();
		}

		this.text.Text = str;
	}
}
