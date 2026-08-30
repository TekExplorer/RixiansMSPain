using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using System;

namespace HideDetailsMod.HideDetailsModCode.Scenes.CharacterSelect;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.BeginRun))]
static public class BeginRunPatch
{
	[HarmonyPrefix]
	static void Prefix()
	{
		NecrobinderBg.Node?.ResetFingersPosition();
	}
}

public partial class NecrobinderBg : Control
{
#nullable disable
	public TextureRect OstyFingers;
	public Control Osty; // Keep as Control to match the container type in your scene file
#nullable restore
	NCharacterSelectScreen? nCharacterSelectScreen;
	Control? InfoPanel => nCharacterSelectScreen?._infoPanel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		nCharacterSelectScreen = this.GetAncestorOfType<NCharacterSelectScreen>();
		Osty = GetNode<Control>("%Osty"); // Corrected type to match the scene node layout
		OstyFingers = GetNode<TextureRect>("%OstyFront");
		OstyFingers.ZIndex = 1;
	}

	internal void ResetFingersPosition()
	{
		OstyFingers.ZIndex = 0;
	}

	static public NecrobinderBg? Node { get; set; }

	public override void _EnterTree()
	{
		Node = this;
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(this, Node)) Node = null;
	}

	// --- Hover / Bobbing Configurations ---
	[Export] public float BobSpeed = 1.0f;     // How fast it floats up and down
	[Export] public float BobAmplitude = 5.0f; // How high/low it floats (in pixels)
	private double _timeAccumulator = 0.0;     // Keeps track of time elapsed


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsInstanceValid(InfoPanel))
		{
			// 1. Calculate the base tracking positions
			Vector2 targetCenterGlobal = InfoPanel.GlobalPosition + (InfoPanel.Size * 0.5f);
			Vector2 localTargetCenter = GetGlobalTransform().AffineInverse() * targetCenterGlobal;
			Vector2 ostyCenterOffset = Osty.Size * 0.5f * Osty.Scale;

			// 2. Accumulate delta time to drive the math wave smoothly
			_timeAccumulator += delta;

			// 3. Calculate the bobbing vertical offset using a Sine wave
			float bobOffset = MathF.Sin((float)_timeAccumulator * BobSpeed) * BobAmplitude;

			// 4. Combine the tracking position with the vertical bobbing offset
			Vector2 finalPosition = localTargetCenter - ostyCenterOffset;
			finalPosition.Y += bobOffset;

			Osty.Position = finalPosition;
		}
	}
}
