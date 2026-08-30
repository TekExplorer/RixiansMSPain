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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsInstanceValid(InfoPanel))
		{
			// 1. Calculate the true center of the target InfoPanel in screen space
			Vector2 targetCenterGlobal = InfoPanel.GlobalPosition + (InfoPanel.Size * 0.5f);

			// 2. Convert that screen center point into local coordinates for this Control
			Vector2 localTargetCenter = GetGlobalTransform().AffineInverse() * targetCenterGlobal;

			// 3. Offset by Osty's own center size (scaled properly) so it is perfectly centered
			Vector2 ostyCenterOffset = Osty.Size * 0.5f * Osty.Scale;

			// 4. Move Osty to the target center, subtracting its own center point
			Osty.Position = localTargetCenter - ostyCenterOffset;
		}
	}
}
