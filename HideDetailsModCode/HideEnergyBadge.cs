using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
public class HideEnergyBadge
{
    [HarmonyPostfix]
    static void MakeEnergyInvisible(ref NCard __instance, ref TextureRect ____energyIcon)
    {
        if (!MyModConfig.HideEnergy) return;
        if (MyModConfig.ExcludeFranticEscape && __instance.Model is FranticEscape) return; 
        ____energyIcon.Visible = false;
    }
}