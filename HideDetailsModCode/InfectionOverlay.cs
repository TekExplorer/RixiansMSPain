using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
class InfectionOverlay
{
    private static bool EnableInfection { get; } = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OverlayPath), MethodType.Getter)]
    static void SwitchToRed(CardModel __instance, ref string __result)
    {
        if (!MyModConfig.UseCustomArt) return;
        if (!EnableInfection) return;
        if (__instance is Infection)
            __result =
                $"res://HideDetailsMod/scenes/cards/overlays/red_{__instance.Id.Entry.ToLowerInvariant()}.tscn";
    }

    // const string path = "res://HideDetailsMod/scenes/overlays/red_infection.tscn";
}