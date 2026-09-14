using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class FuelArt : AlternateCardArt<Fuel>
{
    // UltraKill!
    static CardImg Blood { get; } = new("token/fuel_blood");

    // The prerequisite would be having any of Ironclads blood-related cards
    // (Bloodletting, Bloodwall, Rupture, Hemokinesis) in your deck /
    //  have Refract in your deck
    public override CardImg? Get(Fuel card)
    {
        if (card.Owner.HasCard(Predicate) || card.Owner.HasPower<RupturePower>()) return Blood;
        return null;
    }

    bool Predicate(CardModel card) => card switch
    {
        Bloodletting or BloodWall or Rupture or Hemokinesis => true,
        Refract => true,
        _ => false,
    };
}

