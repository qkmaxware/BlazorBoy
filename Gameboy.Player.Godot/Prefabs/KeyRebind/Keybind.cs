using Godot;
using System;
using System.Linq;

public partial class Keybind : HBoxContainer {

	[Export] public string ActionName;

	private Label KeyName;
	private Button Primary;
	private Button Secondary;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		this.KeyName = GetNode<Label>("Label");
		this.Primary = GetNode<Button>("Primary");
		this.Secondary = GetNode<Button>("Secondary");

		if (!string.IsNullOrEmpty(ActionName)) {
			this.SetAction(ActionName);
		}
	}

	private static readonly string scancode = "scancode";

	private string action;
	private InputEventKey key_primary;
	private InputEventKey key_secondary;

	public void MakeAction(string name) {
		InputMap.AddAction(name);
		this.action = name;
		var e = new InputEventKey();
		InputMap.ActionAddEvent(name, e);
		key_primary = e;
		new InputEventKey();
		InputMap.ActionAddEvent(name, e);
		key_secondary = e;
	}

	public void SetAction(string name) {
		/*if (!InputMap.HasAction(action)) {
			GD.PrintErr("Cannot create rebinding for action ; action '" + name + "' doesn't exist (similar: " + string.Join(',', InputMap.GetActions().Where(x => x.ToString().StartsWith(name)).Select(x => x.ToString())) + ")");
			return;
		}*/

		this.action = name;
		KeyName.Text = name;
		var events = InputMap.ActionGetEvents(name);
		key_primary = events.OfType<InputEventKey>().FirstOrDefault();
		key_secondary = events.OfType<InputEventKey>().Skip(1).FirstOrDefault();

		Primary.Visible = key_primary is not null;
		Secondary.Visible = key_secondary is not null;

		Primary.Text = EventString(key_primary);
		Secondary.Text = EventString(key_secondary);
	}

	private static string EventString(InputEventKey key) {
		if (key is null)
			return string.Empty;
		if (key.Keycode != Key.None)
			return key.AsTextKeycode();
		if (key.PhysicalKeycode != Key.None)
			return key.AsTextPhysicalKeycode();
		return "(Unset)";
	}

    public override void _Input(InputEvent @event) {
        base._Input(@event);

		if (@event is not InputEventKey key_press) {
			return;
		}

		// Cancel rebind
		if (key_press.Keycode == Key.Escape) {
			Primary.ButtonPressed = false;
			Secondary.ButtonPressed = false;
			return;
		}

		// Rebind
		if (Primary.ButtonPressed) {
			if (key_primary is not null) {
				Primary.ButtonPressed = false;
				key_primary.Keycode = key_press.Keycode;
				key_primary.PhysicalKeycode = key_press.PhysicalKeycode;
				Primary.Text = EventString(key_primary);
				GD.Print("Rebound primary event");
			} else {
				GD.PrintErr("No primary event to rebind key to");
			}
		}

		if (Secondary.ButtonPressed) {
			if (key_secondary is not null) {
				Secondary.ButtonPressed = false;
				key_secondary.Keycode = key_press.Keycode;
				key_secondary.PhysicalKeycode = key_press.PhysicalKeycode;
				Secondary.Text = EventString(key_secondary);
				GD.Print("Rebound secondary event");
			} else {
				GD.PrintErr("No secondary event to rebind key to");
			}
		}
    }
}
