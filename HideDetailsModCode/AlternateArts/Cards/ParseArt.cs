using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class ParseArt : AlternateCardArt<Parse>
{
    static CardImg PoorSleep { get; } = new("necrobinder/parse_if_poor_sleep");

    public override CardImg? Get(Parse card)
    {
        return Util.HasCard<PoorSleep>(card.Owner) ? PoorSleep : null;
    }
}
