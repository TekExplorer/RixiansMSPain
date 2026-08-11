using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class LanternKeyArt : AlternateCardArt<LanternKey>
{
    static CardImg Bread { get; } = new("quest/lantern_key_if_bread");

    public override CardImg? Get(LanternKey card)
    {
        return card.Owner.Relics.OfType<Bread>().Any() ? Bread : null;
    }
}