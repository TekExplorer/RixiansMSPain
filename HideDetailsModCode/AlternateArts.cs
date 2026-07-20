using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Runs;

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
        public static void ReloadCard(NCard? nCard) => nCard?.Reload();
        public static void ReloadCard(NCardHolder? holder) => holder?.CardNode?.Reload();
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard._Ready))]
    [HarmonyPatch(typeof(NCard), nameof(NCard.Reload))]
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

    static public void InitCheck()
    {
        foreach (var Art in Arts)
        {
            foreach (var img in Art.AllPathsAsImg)
            {
                if (!img.Exists())
                {
                    MainFile.Logger.Error($"Img {img.Path} does not exist!");
                }
            }
        }
    }

    public static readonly ICardImgFactory[] Arts = [
        new CardImgFactory2<Shiv>(["token/shiv_2", "token/shiv_fanned", "token/shiv_fanned_inky"], card => {
            if (card.HasFanOfKnives) {
                if (card.Enchantment is Inky) return "token/shiv_fanned_inky";
                return "token/shiv_fanned";
            }
            if (MyModConfig.UseBetaShivArt) return "token/shiv_2";
            return null;
        }) {
            WhenPowerApplied = (shiv, _, power, _) => { if (power is FanOfKnivesPower) CardNeedsReload(shiv); },
            WhenCardEnchanted = (shiv, enchantment, _) => { if (enchantment is Inky) CardNeedsReload(shiv); }
        },
        new CardImgFactory2<Predator>("silent/predator_gold_axe", card => Util.HasCard<GoldAxe>(Util.GetOwner(card))),
        new CardImgFactory2<Outbreak>("silent/outbreak_if_noxious_fumes", card => {
            // MainFile.Logger.Debug($"[Alt Art] [Outbreak] Checking for NoxiousFumes");
            var me = Util.GetOwner(card);
            if (me == null) return null;
            return Util.HasCard<NoxiousFumes>(me) || me.HasPower<NoxiousFumesPower>();
        }){
            WhenPowerApplied = (outbreak, _, power, _) => { if (power is NoxiousFumesPower) CardNeedsReload(outbreak); },
            WhenCardGenerated = (outbreak, card) => { if (card is NoxiousFumes) CardNeedsReload(outbreak); }
        },
        new CardImgFactory2<NoxiousFumes>("silent/noxious_fumes_if_outbreak", card => {
            // MainFile.Logger.Debug($"[Alt Art] [NoxiousFumes] Checking for Outbreak");
            var me = Util.GetOwner(card);
            if (me == null) return null;
            return Util.HasCard<Outbreak>(me) || me.HasPower<OutbreakPower>();
        }) {
            WhenPowerApplied = (noxiousFumes, _, power, _) => { if (power is OutbreakPower) CardNeedsReload(noxiousFumes); },
            WhenCardGenerated = (noxiousFumes, card) => { if (card is Outbreak) CardNeedsReload(noxiousFumes); }
        },
        new CardImgFactory2<Accelerant>("silent/poisonless_accelerant", card => {
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
        new CardImgFactory2<CalculatedGamble>("silent/calculated_gamble_no_draw", card => {
            var me = Util.GetOwner(card);
            if (me == null) return null;

            var HasFiddle = me.Relics.Any(relic => relic is Fiddle);
            var HasNoDrawPower = me.HasPower<NoDrawPower>();

            return HasFiddle || HasNoDrawPower;
        }) {
            WhenPowerApplied = (calculatedGamble, _, power, _) => { if (power is NoDrawPower) CardNeedsReload(calculatedGamble); }
        },
        new CardImgFactory2<Monologue>("regent/monologue_if_lunar_blast", card => Util.HasCard<LunarBlast>(Util.GetOwner(card))),
        new CardImgFactory2<MindRot>(["token/mind_rot", "token/mind_rot_regent"], card => {
            return Util.GetOwner(card)?.Character switch
            {
                Ironclad => "token/mind_rot", // TODO: ironclad mind rot
                Silent => "token/mind_rot", // TODO: silent mind rot
                Regent => "token/mind_rot_regent",
                Necrobinder => "token/mind_rot_necrobinder",
                Defect => "token/mind_rot",
                // Null returns "token/mind_rot" which is the defect version
                _ => null,
            };
        }),
        new CardImgFactory2<SpoilsOfBattle>("regent/spoils_of_battle_if_falling_star_played", card => {
            if (card.IsCanonical) return null;
            var me = Util.GetOwner(card);
            if (me == null) return null;
            var PlayedFallingStarThisCombat = CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.Actor == me.Creature && entry.CardPlay.Card is FallingStar);
            return PlayedFallingStarThisCombat;
        }) {
            WhenCardPlayed = (spoilsOfBattle, _, cardPlay) => {if (cardPlay.Card is FallingStar) CardNeedsReload(spoilsOfBattle);}
        },
        new CardImgFactory2<Parry>("regent/parry_alt", card => {
            if (card.IsCanonical) return null;
            if (card.Pile != null) return null; // regular perry version
            // pile is null, and not canonical. probably a shop or something
            if (CardIsBeingInspected(card)) return null;
            return true;
        }) {
            WhenCardInspected = (parry, nCard, _) => Util.ReloadCard(nCard)
        },
        TinkerTimePatch.AltArt,
        new CardImgFactory2<Dowsing>(new List<int>([1,2,3,4,5]).Select(num => $"quest/dowsing_{num}"), card => {
            if (card.IsCanonical) return null;
            var remaining = 5 - card.RoomsEntered;
            return $"quest/dowsing_{Math.Clamp(remaining, 1,5)}";
        }),
        new CardImgFactory2<Melancholy>(new List<int>([0,1,2,3]).Select(num => $"necrobinder/melancholy_cost_{num}"), card => {
            if (card.IsCanonical) return null;
            var cost = card.EnergyCost.GetResolved(); // this includes all effects like borrowed time
            return $"necrobinder/melancholy_cost_{Math.Clamp(cost, 0,3)}";
        }),
        new CardImgFactory2<TheGambit>("colorless/the_gambit_no_block", card => {
            // if no block power, or block would be zero
            // if (card.DynamicVars.Block.IntValue <= 0) return true;
            // TODO: figure out which of these is the correct one.
            if (card.DynamicVars.Block.IntValue <= 0) return true;

            var owner = Util.GetOwner(card);
            if (owner == null)return null;
            if (owner.HasPower<NoBlockPower>()) return true;
            return null;
        }) {
            WhenPowerApplied = (theGambit, _, power, amount) => {
                if (power is NoBlockPower) CardNeedsReload(theGambit);
                if (power is DexterityPower && amount < 0) CardNeedsReload(theGambit);
            }
        },
        new CardImgFactory2<SharedFate>("necrobinder/shared_fate_if_friendship", card => Util.HasCard<Friendship>(Util.GetOwner(card))) {
            WhenPowerApplied = (sharedFate, _, power, _) => { if (power is FriendshipPower) CardNeedsReload(sharedFate); }
        },
        new CardImgFactory2<Bodyguard>("necrobinder/bodyguard_if_protector", card => Util.HasCard<Protector>(Util.GetOwner(card))),
        new CardImgFactory2<DeathsDoor>("necrobinder/deaths_door_if_applied_doom", card => card.WasDoomAppliedThisTurn) {
            WhenPowerApplied = (deathsDoor, _, power, _) => { if (power is DoomPower) CardNeedsReload(deathsDoor); },
        },
        new CardImgFactory2<Parse>("necrobinder/parse_if_poor_sleep", card => Util.HasCard<PoorSleep>(Util.GetOwner(card))),
        new CardImgFactory2<Charge>(["regent/charge_1_draw", "regent/charge_0_draw"], card => {
            if (!CombatManager.Instance.IsInProgress) return null;
            if (card.IsCanonical) return null;
            var owner = Util.GetOwner(card);
            if (owner == null) return null;
            var drawPile = CardPile.Get(PileType.Draw, owner);
            if (drawPile == null) return null;
            return drawPile.Cards.ToArray() switch {
                [] => "regent/charge_0_draw",
                [not null] =>"regent/charge_1_draw",
                _ => null,
            };
        }) {
            WhenCardDrawn = (charge, _, _, _) => {
                var owner = Util.GetOwner(charge);
                if (owner == null) return;
                if (PileType.Draw.GetPile(owner).Cards.Count < 2) CardNeedsReload(charge);
            }
        },
        ClashPatch.AltArt,
        SnapAlt.SnapOstyDiedArt,
        new CardImgFactory2<Concoct>("silent/concoct_if_x", card => {
            var owner = Util.GetOwner(card);
            foreach (var player in card.CombatState?.Players ?? [])
            {
                if (player == owner) continue;
                if (player.Relics.Any(r => r is ChemicalX)) return true;
                var HasXCost = PileType.Hand.GetPile(player).Cards.Any(c => c.EnergyCost.CostsX);
                if (HasXCost) return true;
            }
            return null;
        }) {
            WhenCardDrawn = (concoct, _, drawnCard, _) => {if (drawnCard.EnergyCost.CostsX) CardNeedsReload(concoct);}
        },
        new CardImgFactory2<Demesne>("demesne_if_queen", card => {
            var runState = RunManager.Instance.State;
            if (runState == null) return null;
            var ActHasQueen = runState.Act.AllBossEncounters.Any(boss => boss is QueenBoss);
            return ActHasQueen && card.IsUpgraded;
        }),
    ];
    static class SnapAlt
    {
        static readonly SpireField<Snap, bool> SnapOstyDied = new(() => false);
        // todo: if Osty died this turn
        // TODO: this kinda sucks.
        static public readonly ICardImgFactory SnapOstyDiedArt = new CardImgFactory2<Snap>("necrobinder/snap_if_osty_died", card => SnapOstyDied[card])
        {
            AfterDeath = (snap, _, creature, _) =>
            {
                if (creature.Monster is Osty osty && creature.PetOwner == Util.GetOwner(snap))
                { SnapOstyDied[snap] = true; CardNeedsReload(snap); }
            },
            WhenTurnEnd = (snap, _, side, _) =>
            {
                if (side != CombatSide.Player) return;
                SnapOstyDied[snap] = false; CardNeedsReload(snap);
            },
            WhenTurnStart = (snap, side, _, _) =>
            {
                if (side == CombatSide.Player) return;
                SnapOstyDied[snap] = false; CardNeedsReload(snap);
            },
        };
    }

    // public static readonly AddedNode<NCard, Control> Node = new(static (nCard) =>
    // {
    //     // Use a simple Control container to hold both blades
    //     Control container = new() { Visible = false };

    //     void updateDelegate()
    //     {
    //         if (!GodotObject.IsInstanceValid(nCard) || !GodotObject.IsInstanceValid(container)) return;
    //         if (nCard.Model is MadScience card && card.Type == CardType.Power && card.TinkerTimeRider == TinkerTime.RiderEffect.None)
    //         {
    //             nCard.Reload(); // TODO: optimize. Should make the reload() patch work on UpdateVisuals instead
    //         }
    //         // to something
    //     }

    //     container.TreeEntered += () =>
    //     {
    //         if (!GodotObject.IsInstanceValid(container)) return;
    //         container.GetTree().ProcessFrame += updateDelegate;
    //     };

    //     container.TreeExiting += () =>
    //     {
    //         if (!GodotObject.IsInstanceValid(container)) return;
    //         if (container.GetTree() != null)
    //             container.GetTree().ProcessFrame -= updateDelegate;
    //     };
    //     return container;
    // });
    // static public int CurrentTemporalIndex(int count)
    // {
    //     double secondsPerCard = Math.Max(0.1, TimeSpan.FromSeconds(0.85).TotalSeconds);
    //     double elapsedSeconds = Time.GetTicksMsec() / 1000.0;
    //     return (int)(Math.Floor(elapsedSeconds / secondsPerCard) % count);
    // }
    public enum InspectionState { Opening, Closing, Updating }
    static public IEnumerable<ICardImgFactory> GetAltsFor(CardModel card)
    {
        var found = Arts.Where(alt => alt.IsFor(card));
        MainFile.Logger.Debug($"Found {found.Count()} alts for {card.Id}");
        return found;
    }
}