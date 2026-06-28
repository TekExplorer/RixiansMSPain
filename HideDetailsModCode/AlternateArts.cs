using System.Reflection.Metadata;
using BaseLib.Extensions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public class AlternateArts
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.AllPortraitPaths), MethodType.Getter)]
    static void AllPortraitPaths(CardModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance == null || __result == null) return;
        try
        {
            if (stuff.TryGetValue(__instance.GetType(), out var found))
            {
                var img = found.factory(__instance);
                if (img == null) return;
                __result = [.. __result, img.PortraitPath];
            }
        }
        catch (Exception e) { MainFile.Logger.Error($"Error in AllPortraitPaths: {e}"); }

    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
    static void PortraitPath(CardModel __instance, ref string __result)
    {
        if (__instance == null) return;
        try
        {
            if (stuff.TryGetValue(__instance.GetType(), out var found))
            {
                var res = found.factory(__instance);
                if (res == null) return;
                __result = res.PortraitPath;
            }
        }
        catch (Exception e) { MainFile.Logger.Error($"Error in PortraitPath: {e}"); }

    }
    // PortraitPngPath
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), "PortraitPngPath", MethodType.Getter)]
    static void PortraitPngPath(CardModel __instance, ref string __result)
    {
        if (__instance == null) return;
        try
        {

            if (stuff.TryGetValue(__instance.GetType(), out var found))
            {
                var img = found.factory(__instance);
                if (img == null) return;
                __result = img.PortraitPngPath;
            }
        }
        catch (Exception e) { MainFile.Logger.Error($"Error in PortraitPngPath: {e}"); }
    }
    static Player? GetOwner(CardModel? card)
    {
        if (card == null) return null;
        Player? player = null;
        try
        {

            player ??= card.Owner;
        }
        catch (Exception e) { MainFile.Logger.Warn($"card.Owner errored with: {e}"); }
        try
        {
            player ??= LocalContext.GetMe(card.RunState);
        }
        catch (Exception e) { MainFile.Logger.Warn($"LocalContext.GetMe(card.RunState) errored with: {e}"); }
        return player;

    }

    private static Dictionary<Type, (List<CardImg?> cardImgs, Func<CardModel, CardImg?> factory)> stuff = new()
    {
        [typeof(Predator)] = ([gold_axe_variant], card =>
            {

                var me = GetOwner(card);
                if (me == null) return null;

                if (me.Deck.Cards.OfType<GoldAxe>().Any())
                    return gold_axe_variant;

                return null;
            }

        ),
        [typeof(Shiv)] = ([shiv_variant], card =>
            {
                if (MyModConfig.UseBetaShivArt) return shiv_variant;
                return null;
            }
        ),
    };

    public static CardImg gold_axe_variant = new("predator_gold_axe");
    public static CardImg shiv_variant = new("shiv_2");

    public class CardImg(string path)
    {
        public string PortraitPath { get; } = ImageHelper.GetImagePath($"atlases/card_atlas.sprites/{path}.tres");
        public string PortraitPngPath { get; } = $"res://artist_assets/{path}.png";
        // public string PortraitPngPath { get; } = ImageHelperExtensions.GetModImagePath($"{path}.png");
    }
}