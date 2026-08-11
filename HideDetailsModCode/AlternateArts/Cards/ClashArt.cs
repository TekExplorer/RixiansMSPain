using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

public class ClashArt : AlternateCardArt<Clash>
{
    private static CardImg Playable { get; } = new("event/clash_playable");
    public override CardImg? Get(Clash card)
    {
        var isPlayable = Traverse.Create(card).Property<bool>("IsPlayable").Value;
        return isPlayable ? Playable : null;
    }

    [HarmonyPatch]
    static class ClashPatch
    {
        [HarmonyPatch(typeof(Clash), "OnPlay")]
        static internal bool Prefix(Clash __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            if (MyModConfig.ClashAsGrandFinale)
            {
                __result = OnPlay2(__instance, choiceContext, cardPlay);
                return false;
            }
            return true;
        }
        static async Task OnPlay2(Clash card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            NGrandFinaleVfx? nGrandFinaleVfx = NGrandFinaleVfx.Create(card.Owner.Creature);
            if (nGrandFinaleVfx != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGrandFinaleVfx);
                await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
            }
            ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
            await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).FromCard(card, cardPlay).Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash", tmpSfx: "blunt_attack.mp3").WithHitVfxNode(NGrandFinaleImpactVfx.Create)
                .Execute(choiceContext);
        }
    }
}