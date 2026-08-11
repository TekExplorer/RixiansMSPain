using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class PiercingWailArt : AlternateCardArt<PiercingWail>
{
    static CardImg Shivs { get; } = new("silent/piercing_wail_if_shivs");

    public override CardImg? Get(PiercingWail card)
    {

        if (card.Owner.Potions.Any(potion => potion is CunningPotion)) return Shivs;
        var hand = CardPile.Get(PileType.Hand, card.Owner);
        if (hand == null) return null;
        return hand.Cards.Any(IsOrMakesShivs) ? Shivs : null;
    }

    static bool IsOrMakesShivs(CardModel card)
    {
        if (card.Tags.Contains(CardTag.Shiv)) return true;
        return card switch
        {
            // InfiniteBlades => true, // doesn't trigger this turn
            BladeDance or CloakAndDagger or StormOfSteel or UpMySleeve or
            HiddenDaggers or FanOfKnives or BladeOfInk or LeadingStrike or
            BladeSymphony => true,
            // TODO: check with art author
            KnifeTrap kt when kt.DynamicVars.TryGetValue("CalculatedShivs", out var v) && v is CalculatedVar cv => cv.Calculate(null) > 0,
            _ => false
        };
    }

}
