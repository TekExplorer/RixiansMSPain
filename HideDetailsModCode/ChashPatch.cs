using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Cards;
using static HideDetailsMod.HideDetailsModCode.AlternateArts;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
class ClashPatch
{
    static public ICardImgFactory AltArt = new CardImgFactory2<Clash>("event/clash_playable", static card =>
    {
        try { return (bool)IsPlayableMethod.Invoke(card, [])!; }
        catch { return null; }
    });
    static internal MethodInfo IsPlayableMethod => AccessTools.PropertyGetter(typeof(Clash), "IsPlayable");
    // OnPlay: change vfx to grand finale!
    // Probably needs a Transpiler
}