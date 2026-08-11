using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Runs;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class DemesneArt : AlternateCardArt<Demesne>
{
    static CardImg Queen { get; } = new("necrobinder/demesne_if_queen");

    public override CardImg? Get(Demesne card)
    {
        // Early exit if the card isn't upgraded, skipping the boss list scan entirely
        if (!card.IsUpgraded) return null;

        if (RunManager.Instance.State is not { } runState) return null;

        var ActHasQueen = runState.Act.AllBossEncounters.Any(boss => boss is QueenBoss);

        return ActHasQueen ? Queen : null;
    }
}
