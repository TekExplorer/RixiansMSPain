using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NCard), "ActivateRewardScreenGlow")]
public class RemoveRewardCardRarityGlow
{
    [HarmonyPrefix]
    static bool NoGlow()
    {
        if (MyModConfig.HideCardRewardRarityGlow) return false;
        return true;
    }
}