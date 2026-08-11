using MegaCrit.Sts2.Core.Models.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class DeathsDoorArt : AlternateCardArt<DeathsDoor>
{
    static CardImg AppliedDoom { get; } = new("necrobinder/deaths_door_if_applied_doom");

    public override CardImg? Get(DeathsDoor card)
    {
        return card.WasDoomAppliedThisTurn ? AppliedDoom : null;
    }
}
