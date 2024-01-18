using Godot;
using System;
using System.Linq;
using Qkmaxware.Emulators.Gameboy.Hardware;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class Disassembler : Control {

	[Export] public TextureRenderer Player;

	private RichTextLabel text;

	private Qkmaxware.Vm.LR35902.Disassembler disassembler = new Qkmaxware.Vm.LR35902.Disassembler(Endianness.LittleEndian);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		text = this.GetNode<RichTextLabel>("ScrollContainer/RichTextLabel");
	}

	public void Refresh() {
		text.Text = string.Empty;
		var cart = Player?.Console?.GetCartridge();
		if (cart is not null) {
			var bytes = cart.GetBytes().Skip(0x014F); // End of header at: 0x014F = 335
			foreach (var instr in disassembler.Disassemble(bytes)) {
				if (instr.Operation is not null) {
					var argString = string.Join(' ', instr.Arguments.Select(x => x.ToString("X4")));
					text.Text += $"0x{instr.Address:X4}    {instr.Operation.Name} {argString}" + System.Environment.NewLine;
				} else {
					text.Text += $"0x{instr.Address:X4}    {instr.Opcode:X2}";
				}
			}
		}
	}
}
