using Godot;
using HideDetailsMod.HideDetailsModCode;
using HideDetailsMod.HideDetailsModCode.AlternateArts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
class LuminesceArt : AlternateCardArt<Luminesce>
{
    public override CardImg? Get(Luminesce card) => null;
    static Color LuminesceColor = new(0.314f, 0.784f, 0.471f);
    public override void AfterNCardUpdateVisuals(Luminesce card, NCard node, PileType pileType, CardPreviewMode previewMode)
    {
        var active = card.IsUpgraded || previewMode == CardPreviewMode.Upgrade;

        node.AssertUncommonGlow()?.Modulate = LuminesceColor;
        node.AssertUncommonGlow()?.Visible = active;

        node.AssertRareGlow()?.Modulate = LuminesceColor;
        node.AssertRareGlow()?.Visible = active;

        node._sparkles.Modulate = LuminesceColor;
        node._sparkles.Visible = active;

        // node.CardHighlight.Modulate = active ? LuminesceColor : NCardHighlight.gold;
    }
    public override void OnNCardSubscribed(Luminesce card, NCard node)
    {
        var active = card.IsUpgraded;

        node.AssertUncommonGlow()?.Modulate = LuminesceColor;
        node.AssertUncommonGlow()?.Visible = active;

        node.AssertRareGlow()?.Modulate = LuminesceColor;
        node.AssertRareGlow()?.Visible = active;

        // node.CardHighlight.Modulate = active ? LuminesceColor : NCardHighlight.gold;
    }
    // public override void OnNCardUnsubscribed(Luminesce card, NCard node)
    // {
    //     node.HideRarityGlow();
    //     node.ResetSparkles();
    //     // node.CardHighlight.Modulate = NCardHighlight.gold;
    // }

    public override void AfterNCardModelChanged(CardModel? previousModel, CardModel? newModel, NCard node)
    {
        if (previousModel is Luminesce { IsUpgraded: true } && newModel is not Luminesce { IsUpgraded: true })
        {
            node.RemoveRarityGlow();
            node.ResetSparkles();
        }
    }
}
