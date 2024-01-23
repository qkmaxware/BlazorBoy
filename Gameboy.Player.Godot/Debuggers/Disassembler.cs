using Godot;
using System;
using System.Linq;
using Qkmaxware.Emulators.Gameboy.Hardware;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class Disassembler : Control {

	[Export] public TextureRenderer Player;

	[Export] public PackedScene RowPrefab;

	private Control container;
	private Label pageLabel;

	public int Page {get; private set;}
	public int PageSize = 1000;

	private Qkmaxware.Vm.LR35902.Disassembler disassembler = new Qkmaxware.Vm.LR35902.Disassembler(Endianness.LittleEndian);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		container = this.GetNode<Control>("ScrollContainer/MarginContainer/VBoxContainer");
		pageLabel = this.GetNode<Label>("Navigator/Label");
		pageLabel.Text = Page.ToString();
	}

	public void PrevPage() {
		if (Page > 0) {
			Page --;
			updateListing();
			pageLabel.Text = Page.ToString();
		}
	}
	public void NextPage() {
		Page++;
		updateListing();
		pageLabel.Text = Page.ToString();
	}

	public void Clear() {
		foreach (Node node in container.GetChildren()) {
			if (node is DisassemblerRow) {
				container.RemoveChild(node);
				node.QueueFree();
			}
		}
	}

	public void Refresh() {
		this.Page = 0;
		updateListing();
	}

	private void updateListing() {
		Clear();
		var cart = Player?.Console?.GetCartridge();
		if (cart is not null) {
			var bytes = cart.Bytes; // End of header at: 0x014F = 335, max 1000 entries
			foreach (var instr in disassembler.Disassemble(bytes).Skip(Page * PageSize).Take(PageSize)) {
				var row = RowPrefab.Instantiate<DisassemblerRow>();
				container.AddChild(row);
				row.SetInfo(instr);
			}
		}
	}
}
