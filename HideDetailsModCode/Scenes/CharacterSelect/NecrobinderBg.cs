using Godot;
using HarmonyLib;
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
	// NCharacterSelectScreen? nCharacterSelectScreen;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// nCharacterSelectScreen = this.GetAncestorOfType<NCharacterSelectScreen>();
		// nCharacterSelectScreen.OnEmbarkPressed;
		// nCharacterSelectScreen.Lobby._isBeginningRun;
		OstyFingers = GetNode<TextureRect>("%OstyFingers");
		OstyFingers.ZIndex = 1;
	}
	public TextureRect OstyFingers;
	internal void ResetFingersPosition()
	{
		OstyFingers.ZIndex = 0;
	}

	static public NecrobinderBg? Node;

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
	}
}