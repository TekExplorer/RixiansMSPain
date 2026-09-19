using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards.Silent;

public class NoxiousFumesArt : AlternateCardArt<NoxiousFumes>
{
    static CardImg IfOutbreak { get; } = new("silent/noxious_fumes_if_outbreak");
    public override CardImg? Get(NoxiousFumes card)
    {
        if (card.Owner == null) return null;
        // TODO: remove when 111.0+ become main branch
        if (Util.HasCard<Outbreak>(card.Owner) || card.Owner.HasPowerIdCompat("OUTBREAK_POWER"))
        {
            return IfOutbreak;
        }
        return null;
    }
}