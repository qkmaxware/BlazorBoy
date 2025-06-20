using Godot;
using System;
using Qkmaxware.Emulators.Gameboy;
using Qkmaxware.Emulators.Gameboy.Hardware;
using LcdBitmap = Qkmaxware.Emulators.Gameboy.Hardware.Bitmap;
using System.Linq;
using System.IO;
using Qkmaxware.Emulators.Gameboy.Player;

public partial class GodotBoy : Control, IDebugable {

	[Signal] public delegate void PlaybackStartedEventHandler();
	[Signal] public delegate void PlaybackStoppedEventHandler();
	[Signal] public delegate void GameSavedEventHandler();
	[Signal] public delegate void GameLoadedEventHandler();

	[Export] public bool EnableLoaderControls = true;
	[Export] public bool EnablePlaybackControls = true;
	[Export] public bool AutoloadOptions = true;

	public enum ControlState
	{
		Stopped, Paused, Playing
	}
	private ControlState State {get; set;} = ControlState.Stopped;
	public bool IsPlaying => State == ControlState.Playing;

	[Export] public Control CartControl;
	[Export] public Control PlaybackControl;
	[Export] public OptionButton SaveSlot;
	[Export] public OnscreenControls[] OnscreenControls;
	[Export] public Options Options;

	public Screen Screen { get; private set; }
	public Speakers Speakers {get; private set;}
	public Gameboy Console {get; init;} = new Gameboy();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		if (!EnableLoaderControls) {
			var loader = this.GetNode<Control>("Loader");
			loader.Visible = false;
			if (SaveSlot is not null) {
				SaveSlot.Selected = 0; // Fix to disable the LoadFromSaveSlot feature
			}
			loader.ProcessMode = Node.ProcessModeEnum.Disabled;
		}
		if (!EnablePlaybackControls) {
			var playback = this.GetNode<Control>("Playback");
			playback.Visible = false;
			playback.ProcessMode = Node.ProcessModeEnum.Disabled;
		}
		if (AutoloadOptions && Options is not null)
		{
			Options.LoadConfig();
		}

		if (OnscreenControls is null)
		{
			OnscreenControls = new OnscreenControls[0];
		}

		this.Speakers = this.GetNode<Speakers>("Speakers");

		this.Screen = this.GetNode<Screen>("Screen Layouts");
		const int DesktopLayout = 0;
		const int TouchDesktopLayout = 1;
		const int SkinnedLayout = 2;
		this.Screen.CurrentTab = OS.GetName().ToLower() switch {
			string s when s.Contains("android")		=> SkinnedLayout,
			string s when s.Contains("ios")			=> SkinnedLayout,
			_ => DisplayServer.IsTouchscreenAvailable() switch {
				true								=> TouchDesktopLayout,
				_ 									=> DesktopLayout
			}
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (CartControl is not null) {
			if (State == ControlState.Stopped) {
				CartControl.Visible = true;
			} else {
				CartControl.Visible = false;
			}
		} 
		if (PlaybackControl is not null) {
			if (State == ControlState.Stopped) {
				PlaybackControl.Visible = false;
			} else {
				PlaybackControl.Visible = true;
			}
		}
		
		if (State == ControlState.Playing) {
			PollInput();
			this.Console.DispatchUntilBufferFlush();
			if (this.Console.GPU.HasBufferJustFlushed) {
				// Repaint
				this.Screen.Redraw(this.Console.GPU.GetCanvasImage());
			}
			if (this.Console?.Sound is not null)
				this.Speakers.GenerateAudio(this.Console, delta);
		}
	}

	public void PollInput() {
		// Arrows
		setInput(KeyCodes.Up, 		OnscreenControls.Where(ctrl => ctrl.IsUpPressed()).Any() 	|| 	Godot.Input.IsActionPressed("move_up"));
		setInput(KeyCodes.Down, 	OnscreenControls.Where(ctrl => ctrl.IsDownPressed()).Any() 	|| 	Godot.Input.IsActionPressed("move_down"));
		setInput(KeyCodes.Left, 	OnscreenControls.Where(ctrl => ctrl.IsLeftPressed()).Any() 	|| 	Godot.Input.IsActionPressed("move_left"));
		setInput(KeyCodes.Right, 	OnscreenControls.Where(ctrl => ctrl.IsRightPressed()).Any() || 	Godot.Input.IsActionPressed("move_right"));

		// Buttons
		setInput(KeyCodes.A, 		OnscreenControls.Where(ctrl => ctrl.IsAPressed()).Any() 	|| 	Godot.Input.IsActionJustPressed("button_a"));
		setInput(KeyCodes.B, 		OnscreenControls.Where(ctrl => ctrl.IsBPressed()).Any() 	|| 	Godot.Input.IsActionJustPressed("button_b"));
		setInput(KeyCodes.Start, 	OnscreenControls.Where(ctrl => ctrl.IsStartPressed()).Any() || 	Godot.Input.IsActionJustPressed("button_start"));
		setInput(KeyCodes.Select, 	OnscreenControls.Where(ctrl => ctrl.IsSelectPressed()).Any()|| 	Godot.Input.IsActionJustPressed("button_select"));
	} 
	private void setInput(KeyCodes vkey, bool @default, params Key?[] pkeys) {
		var pressed = @default;
		foreach (var key in pkeys) {
			if (key.HasValue)
				pressed |= Godot.Input.IsKeyPressed(key.Value); 
		}
		if (pressed) {
			Console.Input.KeyDown(vkey);
		} else {
			Console.Input.KeyUp(vkey);
		}
	}

	private string LastCartPath;
	public void LoadCartFromPath(string filepath, string saveDataPath = null) {
		try {
			LastSavePath = null;
			LastCartPath = filepath;
			var cart = new Cartridge(File.ReadAllBytes(filepath));
			insertCart(cart);
			if (saveDataPath is not null)
				LoadSaveFromFile(saveDataPath);
		} catch (Exception e) {
			GD.PushError(e);
		}
	}

	public void LoadCart(Cartridge cart, string saveDataPath = null) {
		LastSavePath = null;
		this.LastCartPath = null;
		insertCart(cart);
		if (saveDataPath is not null)
			LoadSaveFromFile(saveDataPath);
	}


	public void insertCart(Cartridge cart) {
		/*
		1. Check the manufacture code for 01 or 33h (if 33h, also check the new manufacture code for 01).
		2. Add together the characters of the game title.
		3. Look up the value of the sum in a big list of known sums. if it's found in one of the 65 first entries, the position in the list will be used as pointer in the list of palette setups. If the sum is found later in the list, then the 4th character of the game title is looked up in the corresponding column of a table of known 4th game-title-characters. The position in the table + 65 will be used as pointer in the list of palette setups.
		4.The given palette setup is decoded and the color scheme is set.
		var hash = cart.Info.title.Select(character => (int)character).Sum();
		*/
		Stop();
		this.Console?.LoadCartridge(cart);
	}

	public bool IsCartLoaded() => this.Console is not null && this.Console.IsCartridgeLoaded();

	private string? LastSavePath;
	public void LoadSaveFromSlot() {
		if (SaveSlot is not null) {
			var index = SaveSlot.GetItemId(SaveSlot.Selected);

			// Load save
			if (!string.IsNullOrEmpty(LastCartPath) && index > 0) {
				var saveSlotPath = LastCartPath + ".sav" + index;
				if (File.Exists(saveSlotPath)) {
					var saveData = File.ReadAllBytes(saveSlotPath);
					this.Console.RestoreCartRam(saveData);
					GD.Print("Loaded eRAM from: " + saveSlotPath);
					EmitSignal(SignalName.GameLoaded);
				}
				LastSavePath = saveSlotPath;
			}
		}
	}
	public void LoadSaveFromFile(string path) {
		var saveSlotPath = path;
		if (File.Exists(saveSlotPath)) {
			var saveData = File.ReadAllBytes(saveSlotPath);
			this.Console.RestoreCartRam(saveData);
			GD.Print("Loaded eRAM from: " + saveSlotPath);
			EmitSignal(SignalName.GameLoaded);
		}
		LastSavePath = saveSlotPath;
	}

	public void Start() {
		if (this.Console is not null && this.Console.IsCartridgeLoaded()) {
			this.Console.Reset();

			// If we have a save slot selected, a cart loaded, and that cart has a battery then...
			LoadSaveFromSlot(); // By default this will be "No Save"

			this.Screen.Blank();
			this.State = ControlState.Playing;
			EmitSignal(SignalName.PlaybackStarted);
		} else {
			GD.PushError("No cartridge loaded");
		}
	}

	public void Play() {
		if (State == ControlState.Paused) {
			State = ControlState.Playing;
		} else {
			Start();
		}
	}

	public void Pause() {
		if (State == ControlState.Playing) {
			State = ControlState.Paused;
		}
	}

	public void TogglePause() {
		if (State == ControlState.Playing) {
			State = ControlState.Paused;
		} else if (State == ControlState.Paused) {
			State = ControlState.Playing;
		}
	}

	public void Stop() {
		if (this.Console is not null && this.Console.IsCartridgeLoaded()) {
			this.State = ControlState.Stopped;
			Save();
			this.Console.Reset();
			this.Screen.ShowIntro();
			EmitSignal(SignalName.PlaybackStopped);
		}
	}

	public void Save() {
		if (this.Console.SupportsSaves() && LastSavePath is not null) {
			var saveSlotPath = LastSavePath;
			File.WriteAllBytes(saveSlotPath, this.Console.DumpCartRam().ToArray());
			EmitSignal(SignalName.GameSaved);
		}
	}

	public void Restart() {
		Stop();
		Play();
	}

}
