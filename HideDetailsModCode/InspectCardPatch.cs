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
            MainFile.Logger.Info($"Opened Inspector for {____card?.Model?.Id}");
            AlternateArts.UpdateCards.ReloadCard(____card);
        }
    }

    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close))]
    public static class InspectCardPatchClose
    {
        public static void Prefix(NCard ____card)
        {
            NCardBeingInspected[____card] = false;
            var model = ____card.Model;
            if (model != null) CardBeingInspected[model] = false;
            MainFile.Logger.Info($"Closed Inspector for {____card?.Model?.Id}");
            AlternateArts.UpdateCards.ReloadCard(____card);
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
            MainFile.Logger.Info($"Updated Inspector for {____card?.Model?.Id}");
            AlternateArts.UpdateCards.ReloadCard(____card);
        }
    }
}