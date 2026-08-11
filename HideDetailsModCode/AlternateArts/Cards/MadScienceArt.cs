using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class MadScienceArt : AlternateCardArt<MadScience>
{
    static CardImg Curious { get; } = new("event/mad_science_power_curious");
    static CardImg Expertise { get; } = new("event/mad_science_power_expertise");
    static CardImg Improvement { get; } = new("event/mad_science_power_improvement");

    static readonly SpireField<MadScience, TinkerTime.RiderEffect?> VisualRider = new(() => null);

    public override CardImg? Get(MadScience card)
    {
        var rider = VisualRider[card] ?? card.TinkerTimeRider;

        if (rider == TinkerTime.RiderEffect.None && card.Type == CardType.Power)
        {
            MainFile.Logger.Warn("MadScience shouldn't be missing a Rider when displayed as a power!");
        }

        return rider switch
        {
            TinkerTime.RiderEffect.Expertise => Expertise,
            TinkerTime.RiderEffect.Curious => Curious,
            TinkerTime.RiderEffect.Improvement => Improvement,
            _ => null
        };
    }
}