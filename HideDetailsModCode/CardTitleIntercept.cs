using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(CardModel), "Title", MethodType.Getter)]
public class CardTitleIntercept
{
    [HarmonyPostfix]
    static void Intercept(ref CardModel? __instance, ref String __result)
    {
        if (__instance == null) return;
        if (!MyModConfig.HideTitle) return;
        __result = FormattedText(__instance);
    }

    private static string FormattedText(CardModel card)
    {
        if (!card.IsUpgraded) return "";
        if (card.MaxUpgradeLevel <= 1) return "+";
        return $"+{card.CurrentUpgradeLevel}";
    }
}