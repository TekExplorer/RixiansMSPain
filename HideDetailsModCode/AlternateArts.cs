using BaseLib.Extensions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public class AlternateArts
{
    private static readonly CardImg PredatorGoldAxe = new("predator_gold_axe");
    private static readonly CardImg Shiv2 = new("shiv_2");
    private static readonly CardImg PoisonlessAccelerant = new("poisonless_accelerant");
    private static readonly CardImg NoxiousFumesIfOutbreak = new("noxious_fumes_if_outbreak");
    private static readonly CardImg OutbreakIfNoxiousFumes = new("outbreak_if_noxious_fumes");
    private static readonly CardImg MonologueIfLunarBlast = new("monologue_if_lunar_blast");

    public class MindRotted
    {
        public static readonly CardImg Silent = new("token/mind_rot");
        public static readonly CardImg Regent = new("token/mind_rot_regent");
        public static readonly CardImg Necrobinder = new("token/mind_rot");
        public static readonly CardImg Defect = new("token/mind_rot");
        public static readonly CardImg Ironclad = new("token/mind_rot");
        public static readonly List<CardImg> All = [Silent, Regent, Necrobinder, Defect, Ironclad];
    }


    public static Dictionary<Type, (List<CardImg> cardImgs, Func<CardModel, CardImg?> factory)> Cards { get; } = new()
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
        }
        ),
        [typeof(NoxiousFumes)] = ([NoxiousFumesIfOutbreak], card =>
        {
            if (card.IsCanonical) return null;
            var me = GetOwner(card);
            if (me == null) return null;

            var DeckHasOutbreak = CardInDeck<Outbreak>(me);
            var HasOutbreakPower = me.HasPower<OutbreakPower>();
            return (DeckHasOutbreak || HasOutbreakPower) ? NoxiousFumesIfOutbreak : null;
        }
        ),
        [typeof(Outbreak)] = ([OutbreakIfNoxiousFumes], card =>
        {
            if (card.IsCanonical) return null;
            var me = GetOwner(card);
            if (me == null) return null;

            var DeckHasNoxiousFumes = CardInDeck<NoxiousFumes>(me);
            var HasNoxiousFumesPower = me.HasPower<NoxiousFumesPower>();
            return (DeckHasNoxiousFumes || HasNoxiousFumesPower) ? OutbreakIfNoxiousFumes : null;
        }
        ),
        [typeof(MindRot)] = (MindRotted.All, card =>
        {
            if (card.IsCanonical) return null;
            return GetOwner(card)?.Character switch
            {
                Ironclad => MindRotted.Ironclad,
                Silent => MindRotted.Silent,
                Regent => MindRotted.Regent,
                Necrobinder => MindRotted.Necrobinder,
                Defect => MindRotted.Defect,
                _ => null,
            };
        }
        ),
        [typeof(Monologue)] = ([MonologueIfLunarBlast], card =>
        {

            if (card.IsCanonical) return null;
            var me = GetOwner(card);
            if (me == null) return null;

            if (CardInDeck<LunarBlast>(me)) return MonologueIfLunarBlast;
            return null;
        }
        ),
        // [typeof(KnowThyPlace)] = ([KnowThyPlacePlus], card => card.IsUpgraded ? KnowThyPlacePlus : null),
    };

    static bool CardInDeck<T>(Player owner) where T : CardModel => CardInDeck(owner, card => card is T);

    static bool CardInDeck(Player owner, Func<CardModel, bool> predicate) =>
        owner.Piles.Any(pile => pile.Cards.Any(predicate));


    public class CardImg(string path)
    {
        public CardImg(CardModel card) : this($"{card.Pool.Title.ToLowerInvariant()}/{card.Id.Entry.ToLowerInvariant()}") { }
        public static CardImg Upgraded(CardModel card) => new CardImg(card).Upgraded();

        public string PortraitPath { get; } = $"res://images/atlases/card_atlas.sprites/{path}.tres";
        private string _PortraitJpgPath { get; } = $"res://artist_assets/{path}.jpg";
        private string _PortraitPngPath { get; } = $"res://artist_assets/{path}.png";
        public string PortraitPngPath => ResourceLoader.Exists(_PortraitJpgPath) ? _PortraitJpgPath : _PortraitPngPath;

        // public string PortraitPngPath { get; } = ImageHelperExtensions.GetModImagePath($"{path}.png");
        public CardImg Upgraded()
        {
            if (path.EndsWith("_plus")) return this;
            return new(path + "_plus");
        }

        public bool Exists() => ResourceLoader.Exists(PortraitPath);
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
                List<string> result = [.. __result];

                result.AddRange(found.cardImgs.Select(img => img.PortraitPath));

                result.AddRange(
                    found.cardImgs
                        .ConvertAll(img => img.Upgraded())
                        .Where(upgraded => upgraded.Exists())
                        .Select(upgraded => upgraded.PortraitPath)
                );

                var upgraded = CardImg.Upgraded(__instance);
                if (upgraded.Exists()) result.Add(upgraded.PortraitPath);

                __result = result;

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
            var upgraded = CardImg.Upgraded(__instance);
            if (__instance.IsUpgraded && upgraded.Exists()) __result = upgraded.PortraitPath;

            if (Cards.TryGetValue(__instance.GetType(), out var found))
            {
                var img = found.factory(__instance);
                if (img == null) return;
                if (__instance.IsUpgraded && img.Upgraded().Exists()) img = img.Upgraded();
                __result = img.PortraitPath;
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
            var upgraded = CardImg.Upgraded(__instance);
            if (__instance.IsUpgraded && upgraded.Exists()) __result = upgraded.PortraitPngPath;

            if (Cards.TryGetValue(__instance.GetType(), out var found))
            {
                var img = found.factory(__instance);
                if (img == null) return;
                if (__instance.IsUpgraded && img.Upgraded().Exists()) img = img.Upgraded();
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