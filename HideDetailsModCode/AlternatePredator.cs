using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public class AlternatePredator
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.AllPortraitPaths), MethodType.Getter)]
    static void AllPortraitPaths(CardModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance == null) return;
        if (__instance is not Predator) return;
        if (__result == null) return;
        __result = [.. __result, gold_axe_variant];
    }

    [HarmonyPostfix]
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

    // PortraitPngPath
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), "PortraitPngPath", MethodType.Getter)]
    static void PortraitPngPath(CardModel __instance, ref string __result)
    {
        if (__instance == null) return;
        if (__instance is not Predator) return;
        try
        {
            var me = __instance.Owner ?? LocalContext.GetMe(__instance.RunState);
            if (me == null) return;

            if (me.Deck.Cards.OfType<GoldAxe>().Any())
                __result = gold_axe_variant_png;
        }
        catch { }
    }

    // private static Dictionary<Type, (IEnumerable<string?>, Func<CardModel, string?>)> stuff = new()
    // {
    //     [typeof(Predator)] = ([gold_axe_variant], card =>
    //         {
    //             var me = card.Owner ?? LocalContext.GetMe(card.RunState);
    //             if (me == null) return null;

    //             if (me.Deck.Cards.OfType<GoldAxe>().Any())
    //                 return gold_axe_variant;

    //             return null;
    //         }

    //     )
    // };
    static string gold_axe_variant_png = "res://artist_assets/predator_gold_axe.png";
    static string gold_axe_variant = ImageHelper.GetImagePath($"atlases/card_atlas.sprites/predator_gold_axe.tres");
}