using Godot;
using System;

namespace HideDetailsMod.HideDetailsModCode.Scenes.CharacterSelect;

public partial class RegentBg : Control
{
	[Export]
	private Vector2 radius = new(100.0f, 50.0f);

	[Export]
	private float speed = 0.25f;

	[Export]
	private float maxTiltDegrees = 4.0f; // Significantly reduced for a very gentle sway
#nullable disable
	private TextureRect blade;
#nullable restore
	private Vector2 centerPoint;
	private float angle = 0.0f;

	public override void _Ready()
	{
		blade = GetNode<TextureRect>("%Blade");

		if (blade != null)
		{
			centerPoint = blade.Position;
			blade.PivotOffset = blade.Size / 2.0f;
		}
	}

	public override void _Process(double delta)
	{
		if (blade == null) return;

		angle += speed * (float)delta;

		// 1. Oval position
		float x = centerPoint.X + (Mathf.Cos(angle) * radius.X);
		float y = centerPoint.Y + (Mathf.Sin(angle) * radius.Y);
		blade.Position = new Vector2(x, y);

		// 2. Subtle tilt calculation (smoothed way down)
		float tiltFactor = Mathf.Cos(angle);
		float maxTiltRadians = Mathf.DegToRad(maxTiltDegrees);

		// 3. Apply rotation
		blade.Rotation = tiltFactor * maxTiltRadians;
	}
}
