
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(CardModel), "HoverTips", MethodType.Getter)]
// [HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
public class RemoveHoverTips
{
    [HarmonyPostfix]
    static void RemoveTooltips(ref IEnumerable<IHoverTip> __result)
    {
        __result = new List<IHoverTip>();
    }
}