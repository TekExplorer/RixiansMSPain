using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
namespace HideDetailsMod.HideDetailsModCode.Scenes;

public partial class OstyDanceAnimated : Control
{
#if true
	// Since AddedNode doesnt work properly...
	[HarmonyPatch(typeof(NCard), "_Ready")]
	public static class NCardOstyDancePatch
	{
		private const string ScenePath = "res://HideDetailsMod/scenes/cards/osty_dance.tscn";

		private static readonly PackedScene Scene = ResourceLoader.Load<PackedScene>(ScenePath);

		[HarmonyPostfix]
		public static void Postfix(NCard __instance)
		{
			try
			{
				// Prevent duplicate instances if _Ready fires multiple times
				if (__instance.FindChild("OstyDanceAnimated", recursive: false, owned: false) is not null)
				{
					return;
				}

				var danceNode = Scene.Instantiate<OstyDanceAnimated>();
				danceNode.Name = "OstyDanceAnimated";

				// Pass parent card reference if you expose it via a public method or property
				danceNode.card = __instance;

				__instance._portraitCanvasGroup.AddChild(danceNode);
			}
			catch (Exception e)
			{
				MainFile.Logger.Warn("Exception Initializing OstyDanceAnimated: " + e.ToString());
			}
		}
	}
#endif

#if false // baselib bug. next version hopefully fixes it.
	static public readonly AddedNode<NCard, OstyDanceAnimated> Node = new("res://HideDetailsMod/scenes/cards/osty_dance.tscn", static (card, animation) =>
	{
		animation.card = card;
		card._portraitCanvasGroup.AddChild(animation);
	});
#endif

#nullable disable
	NCard card;
	AnimatedSprite2D animation;
#nullable restore

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Position = new(Position.X - 3, Position.Y);

		animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Visible = false;
		if (card == null) return;


		// card.MoveChildSafely(this, card._ancientPortrait.GetIndex() + 1);
		// animation.Size = card._ancientPortrait.Size;
		// animation.Position = card._ancientPortrait.Position;
		// animation.Scale = card._ancientPortrait.Scale;

		// card.RemoveChildSafely(this);
		// card._ancientPortrait.AddChildSafely(this);
		// card._ancientPortrait.AddSiblingSafely(this);

		// card!.ModelChanged += UpdateModel;

		UpdateModel(card.Model);
	}

	void UpdateModel(CardModel? cardModel)
	{
#if DEBUG
		MainFile.Logger.Info($"[OstyDanceAnimated] UpdateModel({cardModel?.Id.Entry ?? "null"}) Config(UseCustomArt: {MyModConfig.UseCustomArt}, UseSimpleMode: {MyModConfig.UseSimpleMode})");
#endif
		if (animation is null) return;
		if (cardModel is PullAggro && MyModConfig.UseCustomArt && !MyModConfig.UseSimpleMode)
		{
			Visible = true;
			if (!animation.IsPlaying()) animation.Play();
		}
		else
		{
			Visible = false;
			if (animation.IsPlaying()) animation.Stop();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// TODO: Could be better
		UpdateModel(card?.Model);
	}
}
