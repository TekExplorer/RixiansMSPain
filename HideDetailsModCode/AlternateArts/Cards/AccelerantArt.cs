using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class AccelerantArt : AlternateCardArt<Accelerant>
{
    static CardImg Poisonless { get; } = new("silent/poisonless_accelerant");

    public override CardImg? Get(Accelerant card)
    {
        var AnyCardInDeckWithPoison = Util.HasCard(card.Owner, Card =>
            Card.DynamicVars.ContainsKey("PoisonPower")
        );

        var HasPoisonRelic = card.Owner.Relics.Any(Relic =>
            Relic is not SneckoSkull && Relic.DynamicVars.ContainsKey("PoisonPower")
        );

        var HasPoison = AnyCardInDeckWithPoison || HasPoisonRelic;

        return HasPoison ? null : Poisonless;
    }
}