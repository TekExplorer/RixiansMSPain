using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public class AlternateArts
{
    private static readonly CardImg PredatorGoldAxe = new("predator_gold_axe");
    private static readonly CardImg Shiv2 = new("shiv_2");
    private static readonly CardImg PoisonlessAccelerant = new("poisonless_accelerant");
    private static readonly CardImg NoxiousFumesIfOutbreak = new("noxious_fumes_outbreak");
    private static readonly CardImg AbrasivePlus = new("abrasive_plus");

    public static Dictionary<Type, (List<CardImg?> cardImgs, Func<CardModel, CardImg?> factory)> Cards { get; } = new()
    {
        [typeof(Shiv)] = ([Shiv2], _ => MyModConfig.UseBetaShivArt ? Shiv2 : null),
        [typeof(Predator)] = ([PredatorGoldAxe], card =>
                {
                    if (card.IsCanonical) return null;
                    var me = GetOwner(card);
                    if (me == null) return null;
                    if (CardInDeck<GoldAxe>(me)) return PredatorGoldAxe;
                    return null;
                }
            ),
        [typeof(Accelerant)] = ([PoisonlessAccelerant], card =>
        {
            if (card.IsCanonical) return null;
            var me = GetOwner(card);
            if (me == null) return null;

            var AnyCardInDeckWithPoison =
                CardInDeck(me, Card => Card.DynamicVars.ContainsKey("PoisonPower"));


            var HasPoisonRelic = me.Relics.Any(Relic =>
                Relic is not SneckoSkull && Relic.DynamicVars.ContainsKey("PoisonPower"));

            var HasPoison = AnyCardInDeckWithPoison || HasPoisonRelic;

            return !HasPoison ? PoisonlessAccelerant : null;
        }),
        [typeof(NoxiousFumes)] = ([NoxiousFumesIfOutbreak], card =>
                {
                    if (card.IsCanonical) return null;
                    var me = GetOwner(card);
                    if (me == null) return null;

                    var DeckHasOutbreak = CardInDeck<Outbreak>(me);

                    return DeckHasOutbreak ? NoxiousFumesIfOutbreak : null;
                }
            ),
        [typeof(Abrasive)] = ([AbrasivePlus], card => card.IsUpgraded ? AbrasivePlus : null),
    };

    static bool CardInDeck<T>(Player owner) where T : CardModel => CardInDeck(owner, card => card is T);

    static bool CardInDeck(Player owner, Func<CardModel, bool> predicate) =>
        owner.Piles.Any(pile => pile.Cards.Any(predicate));


    public class CardImg(string path)
    {
        public string PortraitPath { get; } = ImageHelper.GetImagePath($"atlases/card_atlas.sprites/{path}.tres");

        public string PortraitPngPath { get; } = $"res://artist_assets/{path}.png";
        // public string PortraitPngPath { get; } = ImageHelperExtensions.GetModImagePath($"{path}.png");
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.AllPortraitPaths), MethodType.Getter)]
    static void AllPortraitPaths(CardModel? __instance, ref IEnumerable<string>? __result)
    {
        if (__instance == null || __result == null) return;
        if (!MyModConfig.UseCustomArt) return;
        try
        {
            if (Cards.TryGetValue(__instance.GetType(), out var found))
            {
                var img = found.factory(__instance);
                if (img == null) return;
                __result = [.. __result, img.PortraitPath];
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Error in AllPortraitPaths: {e}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
    static void PortraitPath(CardModel? __instance, ref string __result)
    {
        if (!MyModConfig.UseCustomArt) return;
        if (__instance == null) return;
        try
        {
            if (Cards.TryGetValue(__instance.GetType(), out var found))
            {
                var res = found.factory(__instance);
                if (res == null) return;
                __result = res.PortraitPath;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Error in PortraitPath: {e}");
        }
    }

    // PortraitPngPath
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), "PortraitPngPath", MethodType.Getter)]
    static void PortraitPngPath(CardModel? __instance, ref string __result)
    {
        if (!MyModConfig.UseCustomArt) return;
        if (__instance == null) return;
        try
        {
            if (Cards.TryGetValue(__instance.GetType(), out var found))
            {
                var img = found.factory(__instance);
                if (img == null) return;
                __result = img.PortraitPngPath;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Error in PortraitPngPath: {e}");
        }
    }

    public static Player? GetOwner(CardModel? card)
    {
        if (card == null) return null;
        if (card.IsCanonical) return null;
        Player? player = null;
        try
        {
            player ??= card.Owner;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"card.Owner errored with: {e}");
        }

        try
        {
            player ??= LocalContext.GetMe(card.RunState);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"LocalContext.GetMe(card.RunState) errored with: {e}");
        }

        return player;
    }
}