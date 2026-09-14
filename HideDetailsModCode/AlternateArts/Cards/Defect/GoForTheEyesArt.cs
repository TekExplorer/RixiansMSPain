using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards.Defect;

class GoForTheEyesArt : AlternateCardArt<GoForTheEyes>
{
    static CardImg TargetLocked { get; } = new("defect/go_for_the_eyes_target_locked");
    public override CardImg? Get(GoForTheEyes card)
    {
        var node = card.Node;
        // Only really need node?._previewTarget
        var Target = node?._previewTarget ?? card.CurrentTarget;

        var intendsToAttack = Target?.Monster?.IntendsToAttack;

        if (intendsToAttack == true)
        {
            return TargetLocked;
        }
        return null;
    }
}