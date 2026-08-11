using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

partial class RattleAnimation : Control
{
    static public AddedNode<NCard, RattleAnimation> node = new("HideDetailsMod/scenes/cards/Rattle.tscn",
        (card, animation) => animation.SetCard(card));

    //reference to the animation player node
#nullable disable
    NCard card;
    AnimationPlayer animation_player;

#nullable restore
    private CardModel? model;


    private Tween rattle_tween_animation = new Tween();

    // Fetches the card model (ideally for rattle?)
    void SetCard(NCard card)
    {
        this.card = card;
        model = card.Model;

        MainFile.Logger.Info("Card Set!");
    }

    public override void _Ready()
    {
        //Apply all tween properties

        rattle_tween_animation.TweenProperty(card.Body, "position:x", -600.0f,0.2f);
        rattle_tween_animation.TweenProperty(card.Body, "rotation_degrees", -45.0f,0.2f);
        rattle_tween_animation.Chain().TweenProperty(card.Body, "position:x", 600.0f,0.2f);
        rattle_tween_animation.TweenProperty(card.Body, "rotation_degrees", 45.0f,0.2f);
        rattle_tween_animation.Chain().TweenProperty(card.Body, "position:x", 600.0f,0.2f);
        rattle_tween_animation.TweenProperty(card.Body, "rotation_degrees", 45.0f, 0.2f);
        rattle_tween_animation.Chain().TweenProperty(card.Body, "position:x", 0.0f,0.2f);
        rattle_tween_animation.TweenProperty(card.Body, "rotation_degrees", 0.0f, 0.2f);
        
        card.Body.RemoveChildSafely(this);
        //card.Body.AddChildSafely(node);
        card._ancientPortrait.AddSiblingSafely(this);
        //Manually replace cost, effect text ect. if not hidden
        
        card.AddChildSafely(card._titleLabel);
        card.AddChildSafely(card._descriptionLabel);
        card.AddChildSafely(card._banner);
        card.AddChildSafely(card._energyIcon);
        
        

        UpdateModel(model);
        
        MainFile.Logger.Info("RattleAnimation readied!");
    }

    void UpdateModel(CardModel? cardModel)
    {
        model = cardModel;
        if (rattle_tween_animation is null) return;
        if (model is Rattle)
        {
            Visible = true;
        }
        else
        {
            Visible = false;
            StopAnimation();
        }
    }

    //Allows the rattle animation to loop as many times as osty will hit the taget for.
    public void PlayAndLoopAnimation()
    {
        MainFile.Logger.Info("Playing rattle animation...");
        var no_of_hits = ((CalculatedVar)(model.DynamicVars["CalculatedHits"])).Calculate(null);
        
        for (int i = 0; i <= no_of_hits; i++)
        {
            rattle_tween_animation.Play();
        }
    }

    public void StopAnimation()
    {
        if (rattle_tween_animation.IsRunning()) rattle_tween_animation.Stop();
    }

    public override void _Process(double delta)
    {
        if (model != card?.Model) UpdateModel(card?.Model);

        if (!Visible) return;

        if (model?.Pile?.Type == PileType.Play) PlayAndLoopAnimation();
    }
}