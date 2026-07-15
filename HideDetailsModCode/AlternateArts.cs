using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public class AlternateArts
{
    static bool CardIsBeingInspected(CardModel card)
    {
        if (InspectCardPatch.CardBeingInspected[card]) return true;
        NCard? nCard = NCard.FindOnTable(card);
        if (nCard == null) return false;
        return InspectCardPatch.NCardBeingInspected[nCard];
    }

    public static void CardNeedsReload(CardModel card) => AltArtListenerPatch.NCardNeedsUpdateEvent[card]?.Invoke();
    [HarmonyPatch]
    public class AltArtListenerPatch
    {
        internal static SpireField<CardModel, Action?> NCardNeedsUpdateEvent { get; } = new(() => null);
        internal static NotNullSpireField<NCard, Action> NCardReload { get; } = new((nCard) => () => Util.ReloadCard(nCard));


        [HarmonyPostfix]
        [HarmonyPatch(typeof(NCard), "SubscribeToModel")]
        public static void NCardSubscribeToModel(NCard __instance, CardModel? model)
        {
            if (model != null && __instance.IsInsideTree())
            {
                NCardNeedsUpdateEvent[model] += NCardReload[__instance];
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NCard), "UnsubscribeFromModel")]
        public static void NCardUnsubscribeFromModel(NCard __instance, CardModel? model)
        {
            if (model != null)
            {
                NCardNeedsUpdateEvent[model] -= NCardReload[__instance];
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterPowerAmountChanged))]
        public static void AfterPowerAmountChanged(AbstractModel __instance, PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
        {
            Arts.Do(alt => alt.WhenPowerApplied?.Invoke(__instance, choiceContext, power, amount));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardPlayed))]
        public static void AfterCardPlayed(AbstractModel __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            Arts.Do(alt => alt.WhenCardPlayed?.Invoke(__instance, choiceContext, cardPlay));
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardGeneratedForCombat))]
        public static void AfterCardGeneratedForCombat(AbstractModel __instance, CardModel card, Player? creator)
        {
            Arts.Do(alt => alt.WhenCardGenerated?.Invoke(__instance, card));
        }
    }

    public class Util
    {
        public static void ReloadCombatCards<T>(Player? owner) where T : CardModel
        {
            ReloadCombatCards(owner, card => card is T);
        }
        public static void ReloadCombatCards(Player? owner, Func<CardModel, bool> predicate)
        {
            CombatCardsOf(owner).Where(predicate).Do(ReloadCard);
        }

        public static void ReloadCard(CardModel card)
        {
            var nCard = NCard.FindOnTable(card);
            ReloadCard(nCard);
        }
        public static void ReloadCard(NCard? nCard)
        {
            if (nCard == null) return;
            AccessTools.Method(typeof(NCard), "Reload")?.Invoke(nCard, []);
        }

        public static bool HasCard<T>(Player? owner) where T : CardModel => HasCard(owner, card => card is T);

        public static bool HasCard(Player? owner, Func<CardModel, bool> predicate) => CardsOf(owner).Any(predicate);

        public static IEnumerable<CardModel> CombatCardsOf(Player? player) => CardsOf(player, IncludeDeck: false);

        public static IEnumerable<CardModel> CardsOf(Player? player, bool IncludeDeck = true)
        {
            if (player == null) return [];
            if (CombatManager.Instance.IsInProgress) return CardPile.GetCards(player, IncludeDeck ? AllPiles : AllPilesExceptDeck);
            return IncludeDeck ? CardPile.GetCards(player, PileType.Deck) : [];
        }

        public static PileType[] AllPilesExceptDeck => [PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust, PileType.Play];
        public static PileType[] AllPiles => [PileType.Deck, .. AllPilesExceptDeck];

        public static Player? GetOwner(PowerModel? power) => power?.Owner?.Player;

        public static Player? GetOwner(CardModel? card)
        {
            if (card == null) return null;
            if (card.IsCanonical) return null;
            Player? player = null;
            try
            { player ??= card.Owner; }
            catch (Exception e)
            { MainFile.Logger.Warn($"card.Owner errored with: {e}"); }

            try
            { player ??= LocalContext.GetMe(card.RunState); }
            catch (Exception e)
            { MainFile.Logger.Warn($"LocalContext.GetMe(card.RunState) errored with: {e}"); }

            return player;
        }
    }


    [HarmonyPatch]
    public class TiltAlignment
    {
        public const float AlignmentRotationDegrees = -15f;
        // public const float AlignmentRotationDegrees = -6.28f;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NCard), "Reload")]
        public static void TiltAlignmentReload(NCard __instance)
        {
            if (!GodotObject.IsInstanceValid(__instance)) return;
            if (__instance.Model is not Alignment) return;
            __instance.RotationDegrees -= AlignmentRotationDegrees;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NCard), "Model", MethodType.Setter)]
        public static void FixTiltWhenNotAlignment(NCard __instance, CardModel? value, CardModel? ____model)
        {
            if (!GodotObject.IsInstanceValid(__instance)) return;
            if (value == ____model) return;
            if (____model is Alignment && value is not Alignment) __instance.RotationDegrees = 0;
            // if (____model is not Alignment && value is Alignment) __instance.RotationDegrees -= AlignmentRotationDegrees;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "_Ready")]
    [HarmonyPatch(typeof(NCard), "Reload")]
    public static void MakeGlowGlowier(NCard __instance, ref GpuParticles2D ____sparkles, ref NCardRareGlow? ____rareGlow, ref NCardUncommonGlow? ____uncommonGlow)
    {
        if (!GodotObject.IsInstanceValid(__instance)) return;
        if (!GodotObject.IsInstanceValid(__instance.Body)) return;
        if (!GodotObject.IsInstanceValid(____sparkles)) return;

        static void RemoveRarityGlow(ref NCardRareGlow? ____rareGlow, ref NCardUncommonGlow? ____uncommonGlow, NCard card)
        {
            card.KillRarityGlow();

            card.RemoveChildSafely(____rareGlow);
            ____rareGlow?.QueueFree();
            ____rareGlow = null;

            card.RemoveChildSafely(____uncommonGlow);
            ____uncommonGlow?.QueueFree();
            ____uncommonGlow = null;
        }

        var card = __instance;

        if (card.Model is not Glow)
        {
            ____sparkles.Visible = false;
            RemoveRarityGlow(ref ____rareGlow, ref ____uncommonGlow, card);
            card.CardHighlight.Modulate = NCardHighlight.playableColor;
            return;
        }

        // Glow doesn't need to sparkle tbh.
        ____sparkles.Visible = false;

        if (____rareGlow == null)
        {
            var glow = ____rareGlow = NCardRareGlow.Create();
            if (GodotObject.IsInstanceValid(glow))
            {
                card.Body.AddChildSafely(glow);
                card.Body.MoveChildSafely(glow, 1);
            }
        }

        if (____uncommonGlow == null)
        {
            var glow = ____uncommonGlow = NCardUncommonGlow.Create();
            if (GodotObject.IsInstanceValid(glow))
            {
                card.Body.AddChildSafely(glow);
                card.Body.MoveChildSafely(glow, 1);
            }
        }

        card.CardHighlight.Modulate = NCardHighlight.gold;
    }


    [HarmonyPatch]
    class AnimatedCard
    {
        // static BaseLib.Utils.AddedNode<NCard, Control> thing;
    }

    public record CardImg(string Path)
    {
        public CardImg(CardModel card) : this($"{card.Pool.Title.ToLowerInvariant()}/{card.Id.Entry.ToLowerInvariant()}") { }
        public static CardImg Upgraded(CardModel card) => new CardImg(card).Upgraded();

        public string PortraitPath { get; } = $"res://images/atlases/card_atlas.sprites/{Path}.tres";
        private string _PortraitJpgPath { get; } = $"res://artist_assets/{Path}.jpg";
        private string _PortraitPngPath { get; } = $"res://artist_assets/{Path}.png";
        public string PortraitPngPath => ResourceLoader.Exists(_PortraitJpgPath) ? _PortraitJpgPath : _PortraitPngPath;

        // public string PortraitPngPath { get; } = ImageHelperExtensions.GetModImagePath($"{path}.png");
        public CardImg Upgraded() => Path.EndsWith("_plus") ? this : new(Path + "_plus");

        public bool Exists() => ResourceLoader.Exists(PortraitPath);
    }
    public static readonly CardImgFactory[] Arts = [
        new(typeof(Shiv), "shiv2", _ => MyModConfig.UseBetaShivArt),
        new(typeof(Predator), "predator_gold_axe", card => Util.HasCard<GoldAxe>(Util.GetOwner(card))),
        new(typeof(Outbreak), "outbreak_if_noxious_fumes", card => {
            MainFile.Logger.Info($"[Alt Art] [Outbreak] Checking for NoxiousFumes");
            var me = Util.GetOwner(card);
            if (me == null) return null;
            return Util.HasCard<NoxiousFumes>(me) || me.HasPower<NoxiousFumesPower>();
        }){
            WhenPowerApplied = (model, _, power, _) => {
                if (model is Outbreak outbreak && power is NoxiousFumesPower) CardNeedsReload(outbreak);
            },
            WhenCardGenerated = (model, card) => {
                if (model is Outbreak outbreak && card is NoxiousFumes) CardNeedsReload(outbreak);
            }
        },
        new(typeof(NoxiousFumes), "noxious_fumes_if_outbreak", card => {
            MainFile.Logger.Info($"[Alt Art] [NoxiousFumes] Checking for Outbreak");
            var me = Util.GetOwner(card);
            if (me == null) return null;
            return Util.HasCard<Outbreak>(me) || me.HasPower<OutbreakPower>();
        }) {
            WhenPowerApplied = (model, _, power, _) => {
                if (model is NoxiousFumes noxiousFumes && power is OutbreakPower)  CardNeedsReload(noxiousFumes);
            },
            WhenCardGenerated = (model, card) => {
                if (model is NoxiousFumes noxiousFumes && card is Outbreak) CardNeedsReload(noxiousFumes);
            }
        },
        new(typeof(Accelerant), "poisonless_accelerant", card => {
            var me = Util.GetOwner(card);
            if (me == null) return null;

            var AnyCardInDeckWithPoison =
                Util.HasCard(me, Card => Card.DynamicVars.ContainsKey("PoisonPower"));

            var HasPoisonRelic = me.Relics.Any(Relic =>
                Relic is not SneckoSkull && Relic.DynamicVars.ContainsKey("PoisonPower"));

            var HasPoison = AnyCardInDeckWithPoison || HasPoisonRelic;
            return !HasPoison;
        }
        // TODO: React to other poison sources such as multiplayer
        // TODO: possibly in-flight powers as well
        ),
        new(typeof(CalculatedGamble), "calculated_gamble_no_draw", card => {
            var me = Util.GetOwner(card);
            if (me == null) return null;

            var HasFiddle = me.Relics.Any(relic => relic is Fiddle);
            var HasNoDrawPower = me.HasPower<NoDrawPower>();

            return HasFiddle || HasNoDrawPower;
        }) {
            WhenPowerApplied = (model, _, power, _) => {
                if (model is CalculatedGamble calculatedGamble && power is NoDrawPower) CardNeedsReload(calculatedGamble);
            }
        },
        new(typeof(Monologue), "monologue_if_lunar_blast", card => Util.HasCard<LunarBlast>(Util.GetOwner(card))),
        new(typeof(MindRot), ["token/mind_rot", "token/mind_rot_regent"], card => {
            return Util.GetOwner(card)?.Character switch
            {
                // Currently only the regent version has been added
                Regent => "token/mind_rot_regent",
                // Null returns "token/mind_rot" which is the defect version
                Defect => "token/mind_rot",
                _ => null,
            };
        }),
        new(typeof(SpoilsOfBattle), "regent/spoils_of_battle_if_falling_star_played", card => {
            if (card.IsCanonical) return null;
            var me = Util.GetOwner(card);
            if (me == null) return null;
            var PlayedFallingStarThisCombat = CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.Actor == me.Creature && entry.CardPlay.Card is FallingStar);
            return PlayedFallingStarThisCombat;
        }) {
            WhenCardPlayed = (model, _, cardPlay) => {
                if (model is SpoilsOfBattle spoilsOfBattle && cardPlay.Card is FallingStar) CardNeedsReload(spoilsOfBattle);
            }
        },
        new(typeof(Parry), "parry_alt", card => {
            if (card.IsCanonical) return null;
            if (card.Pile != null) return null; // regular perry version
            // pile is null, and not canonical. probably a shop or something
            if (CardIsBeingInspected(card)) return null;
            return true;
        }) { WhenCardInspected = (nCard, _) => { if (nCard.Model is Parry) Util.ReloadCard(nCard); }}
    ];
    public enum InspectionState { Opening, Closing, Updating }

    public record CardImgFactory(Type CardType, string[] AllPaths, Func<CardModel, string?> Condition)
    {
        public CardImgFactory(Type CardType, string Path, Func<CardModel, bool?> Condition)
            : this(CardType, [Path], card => Condition(card) ?? false ? Path : null) { }

        internal IEnumerable<CardImg> AllPathsAsImg => AllPaths.Select(Path => new CardImg(Path));
        internal IEnumerable<CardImg> AllNormal => AllPathsAsImg.Where(Img => Img.Exists());
        internal IEnumerable<CardImg> AllUpgraded => AllPathsAsImg.Select(Path => Path.Upgraded()).Where(Img => Img.Exists());
        public IEnumerable<CardImg> All => [.. AllNormal, .. AllUpgraded];

        public Action<NCard, InspectionState>? WhenCardInspected { get; set; } = null;
        public Action<AbstractModel, CardModel>? WhenCardGenerated { get; set; } = null;
        public Action<AbstractModel, PlayerChoiceContext, CardPlay>? WhenCardPlayed { get; set; } = null;
        public Action<AbstractModel, PlayerChoiceContext, PowerModel, decimal>? WhenPowerApplied { get; set; } = null;



        public bool IsFor(CardModel card) => CardType.IsInstanceOfType(card);
        public CardImg? Get(CardModel card)
        {
            if (!IsFor(card))
            {
                MainFile.Logger.Error($"Attempted to Get an alt art img for {card.Id} without checking IsFor first");
                return null;
            }
            var result = Condition(card);
            if (result == null) return null;
            return new(result);
        }
    }


    static IEnumerable<CardImgFactory> GetAltsFor(CardModel card)
    {
        var found = Arts.Where(alt => alt.IsFor(card));
        MainFile.Logger.Debug($"Found {found.Count()} alts for {card.Id}");
        return found;
    }
    [HarmonyPatch]
    public class ArtPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), nameof(CardModel.AllPortraitPaths), MethodType.Getter)]
        public static void AllPortraitPaths(CardModel? __instance, ref IEnumerable<string>? __result)
        {
            var card = __instance;
            if (card == null || __result == null) return;
            if (!MyModConfig.UseCustomArt) return;
            try
            {
                List<string> result = [.. __result];

                var found = GetAltsFor(card);

                result.AddRange(found.SelectMany(alt => alt.All).Select(Img => Img.PortraitPath));

                var upgraded = CardImg.Upgraded(card);
                if (upgraded.Exists()) result.Add(upgraded.PortraitPath);
                __result = result;
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Error in AllPortraitPaths: {e}");
            }
        }

        static CardImg ImgFor(CardModel card)
        {
            var factories = GetAltsFor(card);
            foreach (var factory in factories)
            {
                var img = factory.Get(card);
                if (img == null) continue;
                if (card.IsUpgraded && img.Upgraded().Exists()) img = img.Upgraded();
                return img;
            }
            var Img = new CardImg(card);
            if (card.IsUpgraded && Img.Upgraded().Exists()) Img = Img.Upgraded();
            return Img;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
        public static void PortraitPath(CardModel? __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            if (__instance == null) return;
            try
            {
                var Img = ImgFor(__instance);
                if (Img != null) __result = Img.PortraitPath;
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPath: {e}"); }
        }

        // PortraitPngPath
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), "PortraitPngPath", MethodType.Getter)]
        public static void PortraitPngPath(CardModel? __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            if (__instance == null) return;
            try
            {
                var Img = ImgFor(__instance);
                if (Img != null) __result = Img.PortraitPngPath;
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPngPath: {e}"); }
        }
    }
}