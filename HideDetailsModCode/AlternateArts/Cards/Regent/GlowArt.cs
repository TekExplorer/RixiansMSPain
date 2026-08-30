using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

class GlowArt : AlternateCardArt<Glow>
{
    public override CardImg? Get(Glow card) => null;

    public override void AfterNCardUpdateVisuals(Glow card, NCard node, PileType pileType, CardPreviewMode previewMode)
    {
        node.AssertUncommonGlow();
        node.AssertRareGlow();
    }

    public override void OnNCardSubscribed(Glow card, NCard node)
    {
        node.AssertUncommonGlow()?.Modulate = NCardHighlight.gold;
        node.AssertRareGlow()?.Modulate = NCardHighlight.gold;

    }
    // public override void OnNCardUnsubscribed(Glow card, NCard node)
    // {
    //     node.HideRarityGlow();
    // }

    public override void AfterNCardModelChanged(CardModel? previousModel, CardModel? newModel, NCard node)
    {
        if (previousModel is Glow && newModel is not Glow) node.RemoveRarityGlow();
    }
}