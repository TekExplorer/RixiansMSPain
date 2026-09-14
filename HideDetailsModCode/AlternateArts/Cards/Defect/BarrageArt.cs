using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class BarrageArt : AlternateCardArt<Barrage>
{
    static CardImg TenOrbs { get; } = new("defect/barrage_if_ten_orbs");
    static CardImg IfDramaticEntrance { get; } = new("defect/barrage_if_dramatic_entrance");

    public override CardImg? Get(Barrage card)
    {
        if (card.Owner.PlayerCombatState is { OrbQueue.Orbs.Count: >= 10 }) return TenOrbs;
        if (card.Owner.HasCard<DramaticEntrance>()) return IfDramaticEntrance;
        return null;
    }
}
