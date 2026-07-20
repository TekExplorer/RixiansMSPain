using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

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