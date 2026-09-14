using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

class FtlArt : AlternateCardArt<Ftl>
{
    static CardImg NoDraw { get; } = new("defect/ftl_no_draw");
    public override CardImg? Get(Ftl card)
    {
        if (!card.CanDrawCard) return NoDraw;
        return null;
    }
}