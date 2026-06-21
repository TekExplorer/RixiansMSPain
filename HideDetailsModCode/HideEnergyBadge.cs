using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
public class HideEnergyBadge
{
    [HarmonyPostfix]
    static void MakeEnergyInvisible(ref TextureRect ____energyIcon)
    {
        if (!MyModConfig.HideEnergy) return;
        ____energyIcon.Visible = false;
    }
}