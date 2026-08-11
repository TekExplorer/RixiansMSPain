using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class ConcoctArt : AlternateCardArt<Concoct>
{
    private static CardImg IfX { get; } = new("silent/concoct_if_x");

    public override CardImg? Get(Concoct card)
    {
        var players = card.CombatState?.Players ?? RunManager.Instance.State?.Players;
        if (players == null) return null;

        foreach (var player in players)
        {
            if (player == card.Owner) continue;

            // Check relics first (fast memory access)
            if (player.Relics.Any(r => r is ChemicalX)) return IfX;

            // Check hand cards second
            var hand = CardPile.Get(PileType.Hand, player);
            if (hand != null && hand.Cards.Any(c => c.EnergyCost.CostsX)) return IfX;
        }

        return null;
    }
}