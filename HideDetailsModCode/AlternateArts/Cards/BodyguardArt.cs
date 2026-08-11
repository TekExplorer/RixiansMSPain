using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class BodyguardArt : AlternateCardArt<Bodyguard>
{
    static CardImg Protector { get; } = new("necrobinder/bodyguard_if_protector");

    public override CardImg? Get(Bodyguard card)
    {
        return Util.HasCard<Protector>(card.Owner) ? Protector : null;
    }
}
