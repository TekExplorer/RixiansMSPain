using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

class ChaosArt : AlternateCardArt<Chaos>
{
    static CardImg MetamorphosisPlayed { get; } = new("defect/chaos_if_metamorphosis_played");
    public override CardImg? Get(Chaos card)
    {
        var MetamorphosisPlayedThisCombat = CombatManager.Instance.History
                 .CardPlaysFinished.Any(entry => entry.Actor == card.Owner.Creature && entry.CardPlay.Card is Metamorphosis);
        return MetamorphosisPlayedThisCombat ? MetamorphosisPlayed : null;
    }
}