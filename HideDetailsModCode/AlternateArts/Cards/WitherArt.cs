using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class WitherArt : AlternateCardArt<Wither>
{
    static CardImg Wither1 { get; } = new("status/wither1");
    static CardImg Wither2 { get; } = new("status/wither2");
    static CardImg Wither3 { get; } = new("status/wither3");

    public override CardImg? Get(Wither card)
    {
        return card.FakeUpgradeLevel switch
        {
            <= 0 => Wither1,
            1 => Wither2,
            _ => Wither3
        };
    }
}
