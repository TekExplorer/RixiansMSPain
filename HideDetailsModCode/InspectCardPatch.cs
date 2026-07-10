using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace HideDetailsMod.HideDetailsModCode;



[HarmonyPatch]
public static class InspectCardPatch
{
    public static SpireField<CardModel, bool> CardBeingInspected { get; } = new(() => false);
    public static SpireField<NCard, bool> NCardBeingInspected { get; } = new(() => false);

    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open))]
    public static class InspectCardPatchOpen
    {
        //TODO: Resets when toggle upgrade preview
        public static void Postfix(NCard ____card)
        {
            NCardBeingInspected[____card] = true;
            var model = ____card.Model;
            if (model != null) CardBeingInspected[model] = true;
            MainFile.Logger.Debug($"Opened Inspector for {____card.Model?.Id}");
            InvokeForAlts(____card, AlternateArts.InspectionState.Opening);
        }
    }

    static void InvokeForAlts(NCard nCard, AlternateArts.InspectionState state) => AlternateArts.Arts.Do(alt => alt.WhenInspected?.Invoke(nCard, state));


    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close))]
    public static class InspectCardPatchClose
    {
        public static void Prefix(NCard ____card)
        {
            NCardBeingInspected[____card] = false;
            var model = ____card.Model;
            if (model != null) CardBeingInspected[model] = false;
            MainFile.Logger.Debug($"Closed Inspector for {____card.Model?.Id}");
            InvokeForAlts(____card, AlternateArts.InspectionState.Closing);
        }
    }
    [HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
    public static class InspectCardPatchUpdateCardDisplay
    {
        public static void Postfix(NCard ____card)
        {
            NCardBeingInspected[____card] = true;
            var model = ____card.Model;
            if (model != null) CardBeingInspected[model] = true;
            MainFile.Logger.Debug($"Updated Inspector for {____card.Model?.Id}");
            InvokeForAlts(____card, AlternateArts.InspectionState.Updating);
        }
    }
}