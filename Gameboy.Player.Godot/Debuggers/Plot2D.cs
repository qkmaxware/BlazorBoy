using Godot;
using System;
using System.Linq;

namespace Qkmaxware.Emulators.Gameboy.Player;

public partial class Plot2D : Control {
	[Export] public Line2D line;

	[Export] public Label YAxis;
	[Export] public Label YMax;
	[Export] public Label XAxis;
	[Export] public Label XMax;
	[Export] public Label YXMin;

	[Export] public Marker2D TopLeft;
	[Export] public Marker2D TopRight;
	[Export] public Marker2D BottomLeft;
	[Export] public Marker2D BottomRight;
	
	public void Plot(Vector2[] points) {
		var minx = points.Select(pt => pt.X).Min();
		var maxx = points.Select(pt => pt.X).Max();
		var rangex = maxx - minx;

		var miny = points.Select(pt => pt.Y).Min();
		var maxy = points.Select(pt => pt.Y).Max();
		var rangey = maxy - miny;

		var size = new Vector2(
			TopRight.Position.X - TopLeft.Position.X,
			TopLeft.Position.Y - BottomLeft.Position.Y
		);

		YXMin.Text = $"{minx},{miny}";
		YMax.Text = maxy.ToString();
		XMax.Text = maxx.ToString();

		line.Points = points.Select(pt => new Vector2(
			size.X * ((pt.X - minx) / rangex),
			size.Y * ((pt.Y - miny) / rangey)
		)).ToArray();
	}
}
