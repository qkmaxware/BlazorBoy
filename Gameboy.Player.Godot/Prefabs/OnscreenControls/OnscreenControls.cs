using Godot;
using System;

public partial class OnscreenControls : Control {

	[Export] public Joystick Arrows;
	[Export] public TextureButton A;
	[Export] public TextureButton B;
	[Export] public TextureButton Select;
	[Export] public TextureButton Start;

	public bool IsLeftPressed() => Arrows.GetInputVector().X <= -0.5f;
	public bool IsRightPressed() => Arrows.GetInputVector().X >= 0.5f;

	public bool IsDownPressed() => Arrows.GetInputVector().Y >= 0.5f;
	public bool IsUpPressed() => Arrows.GetInputVector().Y <= -0.5f;

	public bool IsAPressed() => A is null ? false : A.ButtonPressed;
	public bool IsBPressed() => B is null ? false : B.ButtonPressed;
	public bool IsSelectPressed() => B is null ? false : Select.ButtonPressed;
	public bool IsStartPressed() => B is null ? false : Start.ButtonPressed;
}
