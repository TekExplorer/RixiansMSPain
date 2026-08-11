using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class DowsingArt : AlternateCardArt<Dowsing>
{
    static CardImg Remains1 { get; } = new("quest/dowsing_1");
    static CardImg Remains2 { get; } = new("quest/dowsing_2");
    static CardImg Remains3 { get; } = new("quest/dowsing_3");
    static CardImg Remains4 { get; } = new("quest/dowsing_4");
    static CardImg Remains5 { get; } = new("quest/dowsing_5");

    public override CardImg? Get(Dowsing card)
    {
        var remaining = Math.Clamp(5 - card.RoomsEntered, 1, 5);
        return remaining switch
        {
            1 => Remains1,
            2 => Remains2,
            3 => Remains3,
            4 => Remains4,
            5 => Remains5,
            _ => null
        };
    }
}
