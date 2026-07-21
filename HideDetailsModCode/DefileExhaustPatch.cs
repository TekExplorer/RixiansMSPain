using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NExhaustPileButton))]
static public class DefileExhaustPatch
{
    // static readonly ICardImgFactory defile = new CardImgFactory2<Defile>([], _ => null)
    // {
    //     WhenCardDrawn = (defile, context, cardDrawn, _) =>
    //     {
    //     },
    // };

    // [HarmonyPostfix, HarmonyPatch(nameof(NExhaustPileButton.AnimIn))]
    // static void AnimIn(NExhaustPileButton __instance)
    // { }

    // don't leave until we allow it.
    [HarmonyPrefix, HarmonyPatch(nameof(NExhaustPileButton.AnimOut))]
    static bool AnimOut(NExhaustPileButton __instance)
    {
        if (CombatManager.Instance.IsEnding) return true;
        if (DefileExhaustIcon.Node.Get(__instance).Visible) return false;
        return true;
    }
}
