using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class NoxiousFumesArt : AlternateCardArt<NoxiousFumes>
{
    static CardImg IfOutbreak { get; } = new("silent/noxious_fumes_if_outbreak");
    public override CardImg? Get(NoxiousFumes card)
    {
        if (Util.HasCard<Outbreak>(card.Owner) || card.Owner.HasPowerCompat("OutbreakPower"))
        {
            return IfOutbreak;
        }
        return null;
    }
}