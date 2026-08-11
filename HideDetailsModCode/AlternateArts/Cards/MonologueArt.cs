using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class MonologueArt : AlternateCardArt<Monologue>
{
    static CardImg IfLunarBlast { get; } = new("regent/monologue_if_lunar_blast");

    public override CardImg? Get(Monologue card)
    {
        return Util.HasCard<LunarBlast>(card.Owner) ? IfLunarBlast : null;
    }
}