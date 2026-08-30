using HideDetailsMod.HideDetailsModCode.Patches;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class ParryArt : AlternateCardArt<Parry>
{
    static ParryArt()
    {
        RunManager.Instance.RunStarted += ResetSeen;
    }
    static void ResetSeen(object? _)
    {
        WasSeen = false;
    }
    static CardImg Alt { get; } = new("regent/parry_alt");
    static bool WasSeen = false;
    public override CardImg? Get(Parry card)
    {
        // MainFile.Logger.Warn($"Parry data: {{WasSeen: {WasSeen}, Inspecting: {IsBeingInspected}, InShop: {IsInShop}, InReward: {IsInCardRewardScreen}}}");
        if (!(card.IsInCardRewardScreen || card.IsInShop)) return null;

        if (card.IsBeingInspected)
        {
            WasSeen = true;
            return null;
        }

        return !WasSeen ? Alt : null;
    }
}