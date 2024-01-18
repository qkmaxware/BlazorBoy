using Godot;
using System;
using Qkmaxware.Emulators.Gameboy;
using Qkmaxware.Emulators.Gameboy.Hardware;
using LcdBitmap = Qkmaxware.Emulators.Gameboy.Hardware.Bitmap;
using System.Linq;
using System.IO;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class TextureRenderer : BitmapTextureRect {

	public enum RendererState {
		Stopped, Paused, Playing
	}
	private RendererState State {get; set;} = RendererState.Stopped;
	public bool IsPlaying => State == RendererState.Playing;
	
	private Gameboy gb = new Gameboy();

	public Gameboy Console => gb;

	private LcdBitmap intro;
	private LcdBitmap white;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		var intro = new LcdBitmap(Gpu.LCD_WIDTH, Gpu.LCD_HEIGHT);
		this.intro = intro;
		intro.Fill(ColourPallet.BackgroundWhite);

		white = new LcdBitmap(Gpu.LCD_WIDTH, Gpu.LCD_HEIGHT);
		white.Fill(ColourPallet.BackgroundWhite);

		var text = new LcdBitmap[]{ LcdBitmap.StampB, LcdBitmap.StampL, LcdBitmap.StampA, LcdBitmap.StampZ, LcdBitmap.StampO, LcdBitmap.StampR, LcdBitmap.StampB, LcdBitmap.StampO, LcdBitmap.StampY }.Select(stamp => stamp.Enlarge(4)).ToArray();
		var width = text.Select(stamp => stamp.Width + 1).Sum();
		var height = text.Select(stamp => stamp.Height).Max();

		var startX = (intro.Width / 2) - (width / 2);
		var startY = (intro.Height / 2) - (height / 2);
		foreach (var stamp in text) {
			intro.Stamp(startX, startY, stamp);
			startX += stamp.Width + 1;
		}

		Redraw(intro);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (State == RendererState.Playing) {
			PollInput();
			gb.DispatchUntilBufferFlush();
			if (gb.GPU.HasBufferJustFlushed) {
				// Repaint
				Redraw(gb.GPU.Canvas);
			}
		}
	}

	public void PollInput() {
		// Arrows
		setInput(KeyCodes.Up, Key.W, Key.Up);
		setInput(KeyCodes.Down, Key.S, Key.Down);
		setInput(KeyCodes.Left, Key.A, Key.Left);
		setInput(KeyCodes.Right, Key.D, Key.Right);

		// Buttons
		setInput(KeyCodes.A, Key.X);
		setInput(KeyCodes.B, Key.Z);
		setInput(KeyCodes.Start, Key.Enter);
		setInput(KeyCodes.Select, Key.Space);

	} 

	private void setInput(KeyCodes vkey, params Key[] pkeys) {
		var pressed = false;
		foreach (var key in pkeys) {
			pressed |= Godot.Input.IsKeyPressed(key); 
		}
		if (pressed) {
			gb.Input.KeyDown(vkey);
		} else {
			gb.Input.KeyUp(vkey);
		}
	}

	public void LoadCartFromPath(string filepath) {
		try {
			LoadCart(new Cartridge(File.ReadAllBytes(filepath)));
		} catch (Exception e) {
			GD.PushError(e);
		}
	}

	public void LoadCart(Cartridge cart) {
		Stop();
		this.gb?.LoadCartridge(cart);
	}

	public void Start() {
		if (this.gb is not null && this.gb.IsCartridgeLoaded()) {
			this.gb.Reset();
			if (white is not null) {
				Redraw(white);
			}
			this.State = RendererState.Playing;
		} else {
			GD.PushError("No cartridge loaded");
		}
	}

	public void Play() {
		if (State == RendererState.Paused) {
			State = RendererState.Playing;
		} else {
			Start();
		}
	}

	public void Pause() {
		if (State == RendererState.Playing) {
			State = RendererState.Paused;
		}
	}

	public void Stop() {
		if (this.gb is not null && this.gb.IsCartridgeLoaded()) {
			this.State = RendererState.Stopped;
			this.gb.Reset();
			if (intro is not null) {
				Redraw(intro);
			}
		}
	}
}
