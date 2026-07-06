// using BaseLib.Utils;
// using HarmonyLib;
// using MegaCrit.Sts2.Core.Models.Cards;
// using MegaCrit.Sts2.Core.Nodes.Cards;

// namespace HideDetailsMod.HideDetailsModCode;

// [HarmonyPatch]
// class NCardHovered
// {
//     public static SpireField<NCard, bool> Hovered = new(() => false);

//     [HarmonyPostfix]
//     [HarmonyPatch(typeof(NCard), "_Ready")]

//     public static void NoteHover(NCard __instance)
//     {
//         __instance.MouseEntered += () => Hovered[__instance] = true;
//         __instance.MouseExited += () => Hovered[__instance] = false;
//     }


//     public static SpireField<NCard, bool> AlignmentTilted = new(() => false);
//     public const float AlignmentRotationDegrees = 15f;

//     [HarmonyPostfix]
//     [HarmonyPatch(typeof(NCard), "Reload")]

//     public static void PrepareHover(NCard __instance)
//     {
//         if (__instance.Model is not Alignment) return;

//         if (AlignmentTilted[__instance] && Hovered[__instance]) return;
//         if (!AlignmentTilted[__instance] && !Hovered[__instance]) return;
//         if (Hovered[__instance] && !AlignmentTilted[__instance])
//         {
//             __instance.RotationDegrees += AlignmentRotationDegrees;
//             AlignmentTilted[__instance] = true;
//         }
//         else
//         {
//             __instance.RotationDegrees -= AlignmentRotationDegrees;
//             AlignmentTilted[__instance] = false;
//         }
//     }
// }