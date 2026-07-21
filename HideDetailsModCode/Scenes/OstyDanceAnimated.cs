using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

public partial class OstyDanceAnimated : Control
{
	static internal AddedNode<NCard, OstyDanceAnimated> _ = new("res://HideDetailsMod/scenes/cards/osty_dance.tscn", (card, animation) => animation.card = card);

	NCard? card;

	AnimatedSprite2D? animation;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animation = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Visible = card?.Model is PullAggro;
	}
}
