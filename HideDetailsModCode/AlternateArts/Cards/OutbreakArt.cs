using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class OutbreakArt : AlternateCardArt<Outbreak>
{
    static CardImg IfNoxious { get; } = new("silent/outbreak_if_noxious_fumes");

    public override CardImg? Get(Outbreak card)
    {
        // MainFile.Logger.Debug($"[Alt Art] [Outbreak] Checking for NoxiousFumes");
        if (Util.HasCard<NoxiousFumes>(card.Owner) || card.Owner.HasPower<NoxiousFumesPower>())
        {
            return IfNoxious;
        }

        return null;
    }
}