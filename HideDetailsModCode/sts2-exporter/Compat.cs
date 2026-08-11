// using System.Collections;
// using System.Reflection;
// using System.Reflection.Emit;
// using BaseLib.Utils;
// using HarmonyLib;
// using HideDetailsMod.HideDetailsModCode;
// using HideDetailsMod.HideDetailsModCode.Patches;
// using MegaCrit.Sts2.Core.Models;
// [HarmonyPatch]
// static class Sts2ExporterCompatPatch
// {
//     static Type CardExportType => AccessTools.TypeByName("STS2Export.Exporter.CardExport");
//     static bool Sts2ExporterExists => CardExportType != null;
//     [HarmonyPrepare] static internal bool Prepare() => Sts2ExporterExists;
//     [HarmonyPatch("STS2Export.Exporter.CardExport", "FindAll")]
//     static class CardExportFindAllPatch
//     {
//         [HarmonyPrepare] static internal bool Prepare() => Sts2ExporterExists;
//         static internal void Postfix(ref IList __result)
//         {
//             // if (!MyModConfig.ShouldPatchCardExporter) return true;
//             var All = ModelDb.AllCards
//                         .Where(m => m.ShouldShowInCardLibrary)
//                         .SelectMany(Stuff.GenerateAllVersionsInternal)
//                         .SelectMany<(CardModel card, Stuff.CustomImgType type), (CardModel, int)>(m =>
//                         {
//                             CardModel Clone(bool upgrade = false)
//                             {
//                                 var clone = (CardModel)m.card.Downgrade().MutableClone();
//                                 if (upgrade) clone.Downgrade();
//                                 return clone;
//                             }
//                             return m.type switch
//                             {
//                                 Stuff.CustomImgType.OnlyBase => [(Clone(), 0)],
//                                 Stuff.CustomImgType.OnlyUpgrade => [(Clone(), 1)],
//                                 Stuff.CustomImgType.Both => [(Clone(), 0)/*, (Clone(), 1)*/],
//                                 _ => throw new NotImplementedException()
//                             };
//                         }
//                         ).ToList();

//             var existing = __result;

//             Type cardType = AccessTools.TypeByName("STS2Export.Exporter.CardExport");
//             // 2. Create the generic list
//             Type listType = typeof(List<>).MakeGenericType(cardType)!;
//             IList cardList = (IList)Activator.CreateInstance(listType)!;

//             var instantiated = All.Select(m => Activator.CreateInstance(cardType, m.Item1, m.Item2));

//             foreach (var item in existing)
//             {
//                 if (item.GetType().Name == "MadScienceExport") cardList.Add(item);
//             }
//             foreach (var item in instantiated)
//             {
//                 cardList.Add(item);
//             }

//             __result = cardList;

//             // return false;
//         }
//     }
//     [HarmonyPatch]
//     public static class AnotherOne
//     {
//         [HarmonyPrepare] static internal bool Prepare() => Sts2ExporterExists;

//         [HarmonyPostfix]
//         [HarmonyPatch("STS2Export.Exporter.ItemList", "FindAll")]
//         static internal void StopWastingMyTime(object __instance)
//         {
//             var traverse = new Traverse(__instance);
//             string[] toClear = ["Relics", "Potions", "Events", "Creatures", "Enchantments", "Keywords", "Afflictions"];
//             foreach (var item in toClear)
//             {
//                 traverse.Field(item).GetValue<IList>().Clear();
//             }
//             // var cards = traverse.Field("Cards").GetValue<IList>();
//         }

//         [HarmonyPrefix]
//         [HarmonyPatch("STS2Export.Exporter.CardExport", "ProcessCombinedDescription")]
//         static internal bool DontBotherWithDesc(ref string __result)
//         {
//             __result = "";
//             return false;
//         }
//         [HarmonyPrefix]

//         [HarmonyPatch("STS2Export.Exporter.CardExport", "Description", MethodType.Getter)]
//         static internal bool DontBotherWithDesc2(ref string __result)
//         {
//             __result = "";
//             return false;
//         }

//         [HarmonyPrefix]

//         [HarmonyPatch("STS2Export.Exporter.ExportBatch", "ExportAllData")]
//         static internal bool ExportAllData()
//         {
//             return false;
//         }

//         static object? Create(CardModel model, int upgrades) => Activator.CreateInstance(CardExportType, model, upgrades);

//         [HarmonyPrefix]

//         [HarmonyPatch("STS2Export.Exporter.CardExport", "UpgradedVersion", MethodType.Getter)]
//         static internal bool UpgradedVersion(ref object? __result, CardModel ___model)
//         {
//             if (Stuff.Types.Get(___model) is { } type)
//             {
//                 if (type == Stuff.CustomImgType.Both && !___model.IsUpgraded)
//                 {
//                     __result = Create(___model.Downgrade(), 1);
//                     return false;
//                 }
//             }
//             __result = null;
//             //   public CardExport UpgradedVersion
//             //   => this.Upgrades < this.model.MaxUpgradeLevel ? new CardExport(this.model.CanonicalInstance, this.Upgrades + 1) : (CardExport) null;

//             //   }
//             return false;
//         }
//     }
//     [HarmonyPatch]
//     public static class CardExportUpgradedVersionPatch
//     {
//         [HarmonyPrepare] static internal bool Prepare() => Sts2ExporterExists;
//         public static CardModel ReplaceCanonical(CardModel card)
//         {
//             // Your custom logic goes here
//             var clone = (CardModel)card.MutableClone();
//             clone.DowngradeInternal();
//             return clone;
//         }

//         [HarmonyTargetMethod]
//         public static MethodBase? TargetMethod()
//         {
//             if (CardExportType == null) return null;

//             // First, check for a property getter
//             MethodInfo getter = AccessTools.PropertyGetter(CardExportType, "UpgradedVersion");
//             if (getter != null) return getter;

//             // Fallback: If it's a standard method or compiler-generated block, find it by name
//             MethodInfo method = AccessTools.Method(CardExportType, "get_UpgradedVersion");
//             return method;
//         }

//         [HarmonyTranspiler]
//         public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//         {
//             var matcher = new CodeMatcher(instructions);

//             // Case-insensitive check targeting any method operand that contains "CanonicalInstance"
//             matcher.MatchStartForward(new CodeMatch(i =>
//                 (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt) &&
//                 i.operand is MethodBase m &&
//                 m.Name.IndexOf("CanonicalInstance", StringComparison.OrdinalIgnoreCase) >= 0
//             ));

//             if (matcher.IsInvalid)
//             {
//                 // Fail out gracefully while logging the raw IL instructions to debug exactly what is wrong
//                 Console.WriteLine("--- HARMONY TRANSPILER FAILURE: RAW IL CODES START ---");
//                 foreach (var ins in instructions)
//                 {
//                     Console.WriteLine($"{ins.opcode} -> {ins.operand} (Type: {ins.operand?.GetType().Name ?? "null"})");
//                 }
//                 Console.WriteLine("--- HARMONY TRANSPILER FAILURE: RAW IL CODES END ---");

//                 throw new InvalidOperationException("Could not locate the CanonicalInstance method invocation inside the bytecode stream.");
//             }

//             // Swap out the call completely with your static method hook
//             MethodInfo replacementMethod = AccessTools.Method(typeof(CardExportUpgradedVersionPatch), nameof(ReplaceCanonical));

//             return matcher
//                 .Set(OpCodes.Call, replacementMethod)
//                 .InstructionEnumeration();
//         }
//     }
//     [HarmonyPatch]
//     static class ExporterIdsPatch
//     {
//         [HarmonyPrepare] static internal bool Prepare() => Sts2ExporterExists;
//         static readonly MethodInfo targetMethod = AccessTools.PropertyGetter("STS2Export.Exporter.CardExport:ID");
//         static internal MethodBase TargetMethod() => targetMethod;

//         static internal void Postfix(ref string __result, CardModel ___model)
//         {
//             var Img = CardImg.Of(___model.PortraitPath);
//             if (Img == null)
//             {
//                 return;
//             }
//             var Canonical = new CardImg(___model);
//             if (Img == Canonical)
//             {
//                 return;
//             }
//             if (Img == Canonical.Upgraded())
//             {
//                 __result += "_plus";
//                 return;
//             }
//             var extra = Img.Path.GetTextAfter(Canonical.Path);
//             if (extra.StartsWith('_')) extra = extra[1..];
//             if (string.IsNullOrEmpty(extra))
//             {
//                 return;
//             }
//             __result = __result + "-" + extra;
//         }
//     }
//     [HarmonyPatch]
//     public static class CardExportConstructorTranspiler
//     {
//         [HarmonyPrepare] static internal bool Prepare() => Sts2ExporterExists;
//         // 1. Target the constructor cleanly using 'null' to wildcard the unknown CardModel type
//         public static MethodBase? TargetMethod()
//         {

//             if (CardExportType == null) return null;
//             // Find the public instance constructor with exactly 2 parameters
//             var ctor = CardExportType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
//                            .FirstOrDefault(c => c.GetParameters().Length == 2);
//             return ctor;
//         }

//         // 2. Transpile using a valid inline predicate match
//         [HarmonyTranspiler]
//         public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//         {
//             return new CodeMatcher(instructions)
//                 // Find ToMutable() callvirt
//                 .MatchStartForward(new CodeMatch(OpCodes.Callvirt, AccessTools.Method("MegaCrit.Sts2.Core.Models.CardModel:ToMutable")))
//                 .ThrowIfInvalid("Could not locate the ToMutable() callvirt instruction.")
//                 // Replace callvirt ToMutable with call MyCustomToMutable
//                 .SetInstruction(
//                      CodeInstruction.Call(typeof(CardExportConstructorTranspiler), nameof(MyCustomToMutable))
//                 )
//                 .InstructionEnumeration();
//         }
//         // Your custom code that runs INSTEAD of ToMutable()
//         public static CardModel MyCustomToMutable(CardModel originalModel)
//         {
//             if (originalModel.IsCanonical) originalModel = originalModel.ToMutable();
//             return originalModel;
//         }
//     }
// }