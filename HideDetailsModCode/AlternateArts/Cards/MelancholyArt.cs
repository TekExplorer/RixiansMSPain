using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class MelancholyArt : AlternateCardArt<Melancholy>
{
    static CardImg Cost0 { get; } = new("necrobinder/melancholy_cost_0");
    static CardImg Cost1 { get; } = new("necrobinder/melancholy_cost_1");
    static CardImg Cost2 { get; } = new("necrobinder/melancholy_cost_2");
    static CardImg Cost3 { get; } = new("necrobinder/melancholy_cost_3");
    public override CardImg? Get(Melancholy card)
    {
        var cost = Math.Clamp(card.EnergyCost.GetResolved(), 0, 3);
        return cost switch
        {
            0 => Cost0,
            1 => Cost1,
            2 => Cost2,
            3 => Cost3,
            _ => null
        };
    }
}