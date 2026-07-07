using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace HideDetailsMod.HideDetailsModCode;

public partial class AlternateArts
{
    public static class InspectCardPatch
    {
        public static SpireField<NCard, bool> CardBeingInspected { get; } = new(() => false);
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open))]
        public static void InspectCardPatchOpen(NCard ____card)
        {
            CardBeingInspected[____card] = true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close))]
        public static void InspectCardPatchClose(NCard ____card)
        {
            CardBeingInspected[____card] = false;
        }
    }
}