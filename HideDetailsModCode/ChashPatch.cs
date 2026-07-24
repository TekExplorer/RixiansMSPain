using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(Clash), "OnPlay")]

static class ClashPatch
{
    static public ICardImgFactory AltArt = new CardImgFactory2<Clash>("event/clash_playable", static card =>
    {
        if (card.IsCanonical) return null;
        return card.CardIsPlayable();
    });
    static internal bool? CardIsPlayable(this CardModel card)
    {
        if (card.IsCanonical) return false;
        return (bool?)IsPlayableMethod.Invoke(card, []);
    }

    static internal MethodInfo IsPlayableMethod => AccessTools.PropertyGetter(typeof(Clash), "IsPlayable");
    // TODO: find a way to guard this against desync
    [HarmonyPrefix]
    public static bool Prefix(
        Clash __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result)
    {
        if (!MyModConfig.ClashAsGrandFinale) return true;
        __result = ExecuteOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    // Total replace. Its fine since Clash should never change.
    private static async Task ExecuteOnPlay(
        Clash instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        NGrandFinaleVfx? nGrandFinaleVfx = NGrandFinaleVfx.Create(instance.Owner.Creature);
        if (nGrandFinaleVfx != null)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGrandFinaleVfx);
            await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(instance.DynamicVars.Damage.BaseValue).FromCard(instance, cardPlay).Targeting(cardPlay.Target)
            .WithHitVfxNode(NGrandFinaleImpactVfx.Create)
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

}