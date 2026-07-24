using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.TestSupport;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public static class InspectCardPatch
{
    static public bool CardWasInspected(CardModel card) => CardInspected[card].Value ?? false;

    internal class Box() { public bool? Value { get; set; } }
    internal static NotNullSpireField<CardModel, Box> CardInspected { get; } = new NotNullSpireField<CardModel, Box>(() => new()).CopyOnClone();


    [HarmonyPatch(typeof(NCardHolder), nameof(NCardHolder.RefreshFocusState))]
    public static class NCardHolderPatch
    {
        static void Postfix(NCardHolder __instance)
        {
            // i hope this is enough :/
            if (__instance._isHovered && __instance.CardModel is Parry) AlternateArts.ParryWasInspected = true;
        }
    }

    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open))]
    public static class InspectCardPatchClose
    {
        public static void Prefix(List<CardModel> cards)
        {
            // hard code it for now
            if (cards.Any(c => c is Parry)) AlternateArts.ParryWasInspected = true;
            foreach (var card in cards) CardInspected[card].Value = false;
            // var model = ____card.Model;
            // MainFile.Logger.Debug($"Closed Inspector for {____card.Model?.Id}");
            // InvokeForAlts(____card, AlternateArts.InspectionState.Closing);
        }
    }

    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.UpdateCardDisplay))]
    public static class InspectCardPatchUpdateCardDisplay
    {
        public static void Prefix(NCard ____card)
        {
            // var model = ____card.Model;
            // if (model != null) CardInspected[model].Value = false;
            // MainFile.Logger.Debug($"Removing card: {____card.Model?.Id} from Inspector");
        }
        public static void Postfix(NCard ____card)
        {
            var model = ____card.Model;
            if (model != null) CardInspected[model].Value = true;
            ____card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            MainFile.Logger.Debug($"Adding card: {____card.Model?.Id} from Inspector");
        }
    }
}