using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class SoulArt : AlternateCardArt<Soul>
{
    static CardImg Freddy { get; } = new("token/beta/soul");
    // static CardImg Wip { get; } = new("token/soul_wip");
    public override CardImg? Get(Soul card)
    {
        return ConfigFrom(card).BetaSoul ? Freddy : null;
    }
}