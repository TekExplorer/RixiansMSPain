// using HarmonyLib;
// using MegaCrit.Sts2.Core.Localization;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Models.Cards;
// using MegaCrit.Sts2.Core.Nodes.Cards;
//
// namespace HideDetailsMod.HideDetailsModCode;
//
// [HarmonyPatch(typeof(CardModel), "Description", MethodType.Getter)]
// public class CardDescriptionIntercept
// {
//     [HarmonyPostfix]
//     static void Intercept(ref CardModel? __instance, ref LocString __result)
//     {
//         if (__instance == null) return;
//         if (!MyModConfig.HideDescription) return;
//         __result = "";
//     }
// }

// Returns a LocString which is way more complex