using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NCard), "UpdateStarCostVisuals")]
public class HideStarBadge
{
    [HarmonyPostfix]
    static void MakeStarsInvisible(ref TextureRect ____starIcon)
    {
        if (!MyModConfig.HideStars) return;
        ____starIcon.Visible = false;
    }
}