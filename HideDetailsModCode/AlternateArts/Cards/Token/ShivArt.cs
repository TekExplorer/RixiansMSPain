using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class ShivArt : AlternateCardArt<Shiv>
{
    static CardImg Beta { get; } = new("token/shiv_2");
    static CardImg Fanned { get; } = new("token/shiv_fanned");
    static CardImg FannedInky { get; } = new("token/shiv_fanned_ink");

    public override IEnumerable<CardImg> GetAll(Shiv card)
    {
        if (MainFile.IsCanary) yield return Beta;
        yield return Fanned;
        yield return FannedInky;
    }

    protected override bool ShowIfCanonical => true;

    public override CardImg? Get(Shiv card)
    {
        if (card.HasFanOfKnives)
        {
            return card.Enchantment is Inky ? FannedInky : Fanned;
        }

        NetModSettings netModSettings = card.IsCanonical ? new() : Util.ConfigFrom(card);

        if (netModSettings.BetaShiv) return Beta;
        return null;
    }
}

