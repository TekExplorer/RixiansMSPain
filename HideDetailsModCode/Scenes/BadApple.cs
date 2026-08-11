using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
namespace HideDetailsMod.HideDetailsModCode.Scenes;

partial class BadApple : Control
{
	static public AddedNode<NCard, BadApple> Node = new("res://HideDetailsMod/scenes/cards/bad_apple.tscn", 
		(card, animation) => animation.SetCard(card));
	// res://HideDetailsMod/scenes/cards/bundle_of_joy.tscn
#nullable disable
	NCard card;
	AnimatedSprite2D animation;
#nullable restore
	CardModel? model;

	void SetCard(NCard card)
	{
		this.card = card;
		model = card.Model;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// TODO: make it animate once on preview
		// MegaCrit.Sts2.Core.Nodes.Cards.Holders.;

		animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animation.AnimationFinished += OnAnimationFinished;

		card.RemoveChildSafely(this);
		card._ancientPortrait.AddSiblingSafely(this);

		UpdateModel(model);
	}

	void UpdateModel(CardModel? cardModel)
	{
		if (!MyModConfig.UseCustomArt && Visible) { Visible = false; Reset(); return; }

		model = cardModel;
		if (animation is null) return;
		if (cardModel is Putrefy)
		{ Visible = true; }
		else
		{
			Visible = false;
			Reset();
		}
	}
	private bool _isGoingForward = true;
	private bool _isSinglePlay = false;
	private void OnAnimationFinished()
	{
		if (_isSinglePlay) return;
		// Toggle the direction tracker
		_isGoingForward = !_isGoingForward;

		if (_isGoingForward)
		{ animation.Play(); }
		else
		{ animation.PlayBackwards(); }
	}

	public void PlayBackAndForth()
	{
		_isSinglePlay = false;
		if (!animation.IsPlaying()) animation.Play();
	}
	public void Reset()
	{
		if (animation.IsPlaying()) animation.Stop();
		_isGoingForward = true;
		animation.Frame = 0;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

		if (model != card?.Model) UpdateModel(card?.Model);

		if (!Visible) return;

		switch (model?.Pile?.Type)
		{
			case PileType.Exhaust: PlayBackAndForth(); break;
			case PileType.Play: PlayBackAndForth(); break;
			default:
				Reset();
				break;
		}
	}
}
