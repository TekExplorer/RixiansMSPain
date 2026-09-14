using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

class MomentumStrikeArt : AlternateCardArt<MomentumStrike>
{
    static CardImg ZeroCost { get; } = new("defect/momentum_strike_0_cost");
    public override CardImg? Get(MomentumStrike card)
    {
        if (card.EnergyCost.GetAmountToSpend() == 0) return ZeroCost;
        return null;
    }
}