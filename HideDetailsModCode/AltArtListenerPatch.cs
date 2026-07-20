using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

public partial class AlternateArts
{
    [HarmonyPatch]
    public class AltArtListenerPatch
    {
        internal static SpireField<CardModel, Action?> NCardNeedsUpdateEvent { get; } = new(() => null);
        internal static NotNullSpireField<NCard, Action> NCardReload { get; } = new((nCard) => nCard.Reload);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NCard), nameof(NCard.SubscribeToModel))]
        public static void NCardSubscribeToModel(NCard __instance, CardModel? model)
        { if (model != null && __instance.IsInsideTree()) NCardNeedsUpdateEvent[model] += NCardReload[__instance]; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NCard), nameof(NCard.UnsubscribeFromModel))]
        public static void NCardUnsubscribeFromModel(NCard __instance, CardModel? model)
        { if (model != null) NCardNeedsUpdateEvent[model] -= NCardReload[__instance]; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterPowerAmountChanged))]
        public static void AfterPowerAmountChanged(AbstractModel __instance, PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
        { Arts.Do(alt => alt.OnPowerApplied(__instance, choiceContext, power, amount)); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardPlayed))]
        public static void AfterCardPlayed(AbstractModel __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        { Arts.Do(alt => alt.OnCardPlayed(__instance, choiceContext, cardPlay)); }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardGeneratedForCombat))]
        public static void AfterCardGeneratedForCombat(AbstractModel __instance, CardModel card, Player? creator)
        { Arts.Do(alt => alt.OnCardGenerated(__instance, card)); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterDeath))]
        public static void AfterDeath(AbstractModel __instance, PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        { Arts.Do(alt => alt.OnDeath(__instance, choiceContext, creature, wasRemovalPrevented)); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterSideTurnEnd))]
        public static void AfterSideTurnEnd(AbstractModel __instance, PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        { Arts.Do(alt => alt.OnTurnEnd(__instance, choiceContext, side, participants)); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterSideTurnStart))]
        public static void AfterSideTurnStart(AbstractModel __instance, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        { Arts.Do(alt => alt.OnTurnStart(__instance, side, participants, combatState)); }
    }
}