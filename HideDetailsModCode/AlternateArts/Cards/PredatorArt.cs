using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class PredatorArt : AlternateCardArt<Predator>
{
    static CardImg WithGoldAxe { get; } = new("silent/predator_gold_axe");
    public override CardImg? Get(Predator card) =>
        Util.HasCard<GoldAxe>(card.Owner) ? WithGoldAxe : null;
}