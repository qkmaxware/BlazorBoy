using Godot;
using System;

public partial class Joystick : Control {

	[Export] public int SnapSpeed = 25;
	[Export] public int DeadZone = 5;
	private Control knob;

	private bool pressing = false;
	private int maxLength = 50;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		knob = this.GetNode<Control>("Knob");
		maxLength = (int)this.Size.X/2;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		var center = this.GlobalPosition + this.Size * 0.5f;
		var knob_offset = knob.Size * 0.5f;

		if (!this.pressing) {
			knob.GlobalPosition = knob.GlobalPosition.Lerp(center - knob_offset, (float)delta*SnapSpeed);
			return;
		}

		var mouse_pos = this.GetGlobalMousePosition();
		var distance = mouse_pos.DistanceTo(center);
		if (distance > maxLength) {
			var angle = center.AngleToPoint(mouse_pos);
			mouse_pos.X = center.X + Mathf.Cos(angle) * maxLength;
			mouse_pos.Y = center.Y + Mathf.Sin(angle) * maxLength;
		}
			
		knob.GlobalPosition = mouse_pos - knob_offset;
	}

	public Vector2 GetInputVector() {
		if (!this.pressing) {
			return Vector2.Zero;
		}

		var center = this.GlobalPosition + this.Size * 0.5f;
		var mouse_pos = this.GetGlobalMousePosition();
		var distance = mouse_pos.DistanceTo(center);
		var vector = mouse_pos - center;
		
		// Clamp values & Deadzone
		if (Mathf.Abs(vector.X) <= this.DeadZone) {
			vector.X = 0;
		}
		vector.X = Mathf.Clamp(vector.X, -maxLength, maxLength);
		if (Mathf.Abs(vector.Y) <= this.DeadZone) {
			vector.Y = 0;
		}
		vector.Y = Mathf.Clamp(vector.Y, -maxLength, maxLength);
		
		return vector / maxLength;
	}

	public void OnButtonDown() {
		pressing = true;
	}

	public void OnButtonUp() {
		pressing = false;
	}

}