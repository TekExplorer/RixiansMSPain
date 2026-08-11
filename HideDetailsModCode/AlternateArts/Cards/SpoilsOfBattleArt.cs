using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class SpoilsOfBattleArt : AlternateCardArt<SpoilsOfBattle>
{
    static CardImg FallingStarPlayed { get; } = new("regent/spoils_of_battle_if_falling_star_played");

    public override CardImg? Get(SpoilsOfBattle card)
    {
        var PlayedFallingStarThisCombat = CombatManager.Instance.History
            .CardPlaysFinished.Any(entry => entry.Actor == card.Owner.Creature && entry.CardPlay.Card is FallingStar);
        return PlayedFallingStarThisCombat ? FallingStarPlayed : null;
    }
}
