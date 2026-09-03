using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using HideDetailsMod.HideDetailsModCode.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

[HarmonyPatch(typeof(AbstractModel))]
static class CustomVfxListenerPatch
{
    static readonly NotNullSpireField<MultiplayerScalingModel, CustomVfxListener> Listener = new(() => new());

    [HarmonyPatch(nameof(AbstractModel.AfterDamageGiven))]
    [HarmonyPrefix]
    static bool AfterDamageGiven(AbstractModel __instance, ref Task __result, PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (__instance is not MultiplayerScalingModel model) return true;
        __result = Listener[model].AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        return false;
    }

    [HarmonyPatch(nameof(AbstractModel.AfterSideTurnStart))]
    [HarmonyPrefix]
    static bool AfterSideTurnStart(AbstractModel __instance, ref Task __result, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (__instance is not MultiplayerScalingModel model) return true;
        __result = Listener[model].AfterSideTurnStart(side, participants, combatState);
        return false;
    }
}

// TODO: use this when beta becomes main, then delete the above
// class CustomVfxListener() : CustomSingletonModel(HookType.Combat)
class CustomVfxListener()
{
    public async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (MyModConfig.UseSimpleMode) return;

        if (cardSource is Squeeze squeeze)
        {
            var nCard = NCard.FindOnTable(squeeze);
            if (nCard == null) return;
            NCardCustomVfxContainer.Node[nCard].PlaySqueeze(.5f);

            NCreature? creature = NCombatRoom.Instance?.GetCreatureNode(target);
            if (creature == null) return;
            if (SqueezeVfxs.ContainsKey(creature)) return;

            // Squeeze inward at the waist and stretch vertically
            NSqueezeVfx? vfx = NSqueezeVfx.Create(
                creature.Visuals,
                mode: NCreatureModifierVfx.DurationMode.UntilRevert
            );
            if (vfx != null)
            {
                CreaturesWithVfx.Add(creature);
                SqueezeVfxs[creature] = vfx;
                await vfx.ApplyTask;
            }
        }
        if (cardSource is Flatten flatten)
        {
            var nCard = NCard.FindOnTable(flatten);
            if (nCard == null) return;
            NCardCustomVfxContainer.Node[nCard].PlayFlatten(.5f);

            NCreature? creature = NCombatRoom.Instance?.GetCreatureNode(target);
            if (creature == null) return;
            if (FlattenVfxs.ContainsKey(creature)) return;

            // Squeeze inward at the waist and stretch vertically
            var vfx = NFlattenVfx.Create(
                creature.Visuals,
                mode: NCreatureModifierVfx.DurationMode.UntilRevert
            );

            if (vfx != null)
            {
                CreaturesWithVfx.Add(creature);
                FlattenVfxs[creature] = vfx;
                await vfx.ApplyTask;
            }
        }
        if (cardSource is Rattle rattle)
        {
            var nCard = NCard.FindOnTable(rattle);
            if (nCard == null) return;
            NCardCustomVfxContainer.Node[nCard].PlayRattle(1f, 100);

            NCreature? creature = NCombatRoom.Instance?.GetCreatureNode(target);
            if (creature == null) return;

            // Squeeze inward at the waist and stretch vertically
            var vfx = NRattleVfx.Create(
                creature.Visuals,
                mode: NCreatureModifierVfx.DurationMode.Timed
            );
            if (vfx != null)
            {
                await (vfx.VfxTask ?? vfx.ApplyTask);
            }
        }
    }
    Dictionary<NCreature, NCreatureModifierVfx> FlattenVfxs { get; } = [];
    Dictionary<NCreature, NCreatureModifierVfx> SqueezeVfxs { get; } = [];
    HashSet<NCreature> CreaturesWithVfx { get; } = [];
    public Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (var creature in CreaturesWithVfx)
        {
            NCreatureModifierVfx.ClearAll(creature.Visuals, animateRevert: true);
        }
        FlattenVfxs.Clear();
        SqueezeVfxs.Clear();
        return Task.CompletedTask;
    }

}