using Godot;
using System;
using System.Linq;

public partial class Options : Window {

	public override void _Ready() {
		//LoadConfig();
		this.CloseRequested += SaveConfig;		
	}


	[Export] public OptionButton DisplayColourPicker;
	[Export] public ColorPickerButton SkinColourPicker;
	[Export] public Keybind[] KeyBindings; // Relies on stuff I don't have on this PC Need to add a way to examine the primary key & secondary key

	public static readonly string DefaultFilePath = "user://user_config.cfg";
	private string FilePath = DefaultFilePath;

	public void SetConfigPath(string path)
	{
		this.FilePath = path ?? DefaultFilePath;
	}

	public void SaveConfig()
	{
		// Create config
		var config = new ConfigFile();

		// Store values
		config.SetValue("theme", "display_colour", DisplayColourPicker.Selected);
		config.SetValue("theme", "skin_colour", SkinColourPicker.Color);

		foreach (var rebind in KeyBindings)
		{
			config.SetValue("keybind", rebind.ActionName, string.Join(',', rebind.GetPrimaryKey().ToString(), rebind.GetSecondaryKey().ToString())); // Add these functions
		}

		// Save to a file
		config.Save(FilePath);
	}

	public void LoadConfig()
	{
		// Create config
		var config = new ConfigFile();

		Error err = config.Load(FilePath);
		if (err != Error.Ok)
		{
			return;
		}

		LoadConfig(config);
	}

	public void LoadConfig(ConfigFile config)
	{
		set(config, "theme", "display_colour", (v) => { DisplayColourPicker.Selected = v.AsInt32(); DisplayColourPicker.EmitSignal("item_selected", v.AsInt32()); });
		set(config, "theme", "skin_colour", (v) => { SkinColourPicker.Color = v.AsColor(); SkinColourPicker.EmitSignal("color_changed", v.AsColor()); });

		foreach (var rebind in KeyBindings)
		{
			set(config, "keybind", rebind.ActionName, (v) =>
			{
				if (v.VariantType != Variant.Type.String)
					return;

				var str = v.AsString().Split(',', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				Enum.TryParse<Key>(str.FirstOrDefault() ?? string.Empty, ignoreCase: true, out Key primary);
				Enum.TryParse<Key>(str.Skip(1).FirstOrDefault() ?? string.Empty, ignoreCase: true, out Key secondary);
				rebind.SetKeys(primary, secondary); // Add this function
			});
		}
	}

	private static void set(ConfigFile config, string section, string key, Action<Variant> setter) {
		try {
			if (!config.HasSectionKey(section, key)) {
				return;
			}
		
			Variant v = config.GetValue(section, key);
			setter(v);
			return;
		} catch {
			return;
		}
	}
}