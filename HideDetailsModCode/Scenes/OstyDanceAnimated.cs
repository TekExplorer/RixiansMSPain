using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
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
		animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Visible = false;
		if (card == null) return;

		// card.MoveChildSafely(this, card._ancientPortrait.GetIndex() + 1);
		// animation.Size = card._ancientPortrait.Size;
		// animation.Position = card._ancientPortrait.Position;
		// animation.Scale = card._ancientPortrait.Scale;
		card.RemoveChildSafely(this);
		// card._ancientPortrait.AddChildSafely(this);
		card._ancientPortrait.AddSiblingSafely(this);

		// card!.ModelChanged += UpdateModel;
		UpdateModel(card.Model);
	}

	void UpdateModel(CardModel? cardModel)
	{
		if (animation is null) return;
		if (cardModel is PullAggro)
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
		UpdateModel(card?.Model);
	}
}
