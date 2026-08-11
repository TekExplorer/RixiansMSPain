using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class ShivArt : AlternateCardArt<Shiv>
{
    static CardImg Beta { get; } = new("token/shiv_2");
    static CardImg Fanned { get; } = new("token/shiv_fanned");
    static CardImg FannedInky { get; } = new("token/shiv_fanned_ink");
    public override CardImg? Get(Shiv card)
    {
        if (card.HasFanOfKnives)
        {
            return card.Enchantment is Inky ? FannedInky : Fanned;
        }
        if (ConfigFrom(card).BetaShiv) return Beta;
        return null;
    }
}

