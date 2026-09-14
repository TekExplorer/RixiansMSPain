using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards.Defect;

class SmokestackArt : AlternateCardArt<Smokestack>
{
    // if automation, production or catastrophe are in the deck
    static CardImg Industry { get; } = new("defect/smokestack_industry");
    public override CardImg? Get(Smokestack card)
    {
        if (card.Owner.HasCard(card => card is Automation or Production or Catastrophe)) return Industry;
        return null;
    }
}