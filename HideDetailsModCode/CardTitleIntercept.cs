using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(CardModel), "Title", MethodType.Getter)]
public class CardTitleIntercept
{
    [HarmonyPostfix]
    static void Intercept(ref CardModel? __instance, ref String __result)
    {
        if (__instance == null) return;
        __result = FormattedText(__instance);
    }

    private static string FormattedText(CardModel card)
    {
        var formattedText = card.TitleLocString.GetFormattedText();
        
        if (MyModConfig.HideTitle) formattedText = "";
        
        if (!card.IsUpgraded)
            return formattedText;
        if (card.MaxUpgradeLevel <= 1)
            return formattedText + "+";
        return $"{formattedText}+{card.CurrentUpgradeLevel}";
    }
}