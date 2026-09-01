// using HarmonyLib;
// using MegaCrit.Sts2.Core.Models.Cards;
// using System.Collections.Generic;
// using System.Reflection;

// namespace HideDetailsMod.HideDetailsModCode.Interop;

// [HarmonyPatch]
// static class DisableSlayTheRelicsExporter
// {
//     static Type? StateExporter = AccessTools.TypeByName("SlayTheRelicsExporter.StateExporter");
//     static IEnumerable<MethodInfo> Methods = new MethodInfo?[] {
//             StateExporter.GetMethod("ExportDeck"),
//             StateExporter.GetMethod("ExportPile"),
//             StateExporter.GetMethod("PopulateCardMeta"),
//         }.OfType<MethodInfo>();
//     static bool Prepare() => AccessTools.Method("SlayTheRelicsExporter.StateExporter:ExportDeck") != null;
//     // Harmony executes this first to find what method to patch
//     public static IEnumerable<MethodBase> TargetMethods()
//     {
//         if (StateExporter == null) return [];
//         MainFile.Logger.Info("SlayTheRelicsExporter.StateExporter Found! Blocking execution of ExportDeck, ExportPile, and PopulateCardMeta");
//         return Methods;
//     }

//     // Disable SlayTheRelics deck exporting
//     // Returning false skips the original void method
//     public static bool Prefix() => false;
// }
