using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public class AlternatePredator
{
    // [HarmonyPatch(typeof(Predator), nameof(Predator.AllPortraitPaths), MethodType.Getter)]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.AllPortraitPaths), MethodType.Getter)]
    [HarmonyPostfix]
    static void AllPortraitPaths(CardModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance == null) return;
        if (__instance is not Predator) return;
        if (__result == null) return;
        __result = [.. __result, gold_axe_variant];
    }

    [HarmonyPostfix]
    // [HarmonyPatch(typeof(Predator), nameof(Predator.PortraitPath), MethodType.Getter)]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
    static void PortraitPath(CardModel __instance, ref string __result)
    {
        if (__instance == null) return;
        if (__instance is not Predator) return;
        try
        {
            var me = __instance.Owner ?? LocalContext.GetMe(__instance.RunState);
            if (me == null) return;

            if (me.Deck.Cards.OfType<GoldAxe>().Any())
                __result = gold_axe_variant;
        }
        catch { }
    }

    static string gold_axe_variant = ImageHelper.GetImagePath($"atlases/card_atlas.sprites/predator_gold_axe.tres");
}