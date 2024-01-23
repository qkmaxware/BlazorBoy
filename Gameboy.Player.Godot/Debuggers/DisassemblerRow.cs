using Godot;
using System;
using System.Linq;
using Qkmaxware.Emulators.Gameboy.Hardware;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class DisassemblerRow : HBoxContainer {

	private InstructionInfo info;

	private Label addressLabel;
	private Label instructionLabel;
	private Label argsLabel;

	const string EmptyArgument = "--";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.addressLabel = GetNode<Label>("Address/Label");
		this.instructionLabel = GetNode<Label>("Instruction/Label");
		this.argsLabel = GetNode<Label>("Args/Label");
	}

	public void SetInfo(InstructionInfo info) {
		this.info = info;

		AddressModeHex();
		InstructionModeName();
		ArgModeDecimal();
	}

	public void AddressModeDecimal() {
		this.addressLabel.Text = info.Address.ToString();
	}
	public void AddressModeHex() {
		this.addressLabel.Text = "0x" + info.Address.ToString("X4");
	}

	public void InstructionModeDecimal() {
		this.instructionLabel.Text = info.Opcode.ToString();
	}
	public void InstructionModeHex() {
		this.instructionLabel.Text = "0x" + info.Opcode.ToString("X2");
	}
	public void InstructionModeName() {
		this.instructionLabel.Text = info.Operation?.Name ?? info.Opcode.ToString();
	}

	public void ArgModeDecimal() {
		if (info.Arguments is null || info.Arguments.Length == 0) {
			this.argsLabel.Text = EmptyArgument;
			return;
		}
		this.argsLabel.Text = string.Join(' ', info.Arguments.Select(x => x.ToString()));
	}
	public void ArgModeHex() {
		if (info.Arguments is null || info.Arguments.Length == 0) {
			this.argsLabel.Text = EmptyArgument;
			return;
		}
		this.argsLabel.Text = string.Join(' ', info.Arguments.Select(x => "0x" + x.ToString("X2")));
	}
}
