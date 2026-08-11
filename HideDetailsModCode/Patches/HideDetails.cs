using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using static MegaCrit.Sts2.Core.Models.CardModel;

namespace HideDetailsMod.HideDetailsModCode.Patches;

[HarmonyPatch]
internal class HideDetailsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateEnergyCostVisuals))]
    internal static void MakeEnergyInvisible(ref NCard __instance, ref TextureRect ____energyIcon)
    {
        bool FranticEscapeExclusion = MyModConfig.ExcludeFranticEscape && __instance.Model is FranticEscape;
        if (!MyModConfig.HideEnergy || FranticEscapeExclusion) return;
        ____energyIcon.Visible = false;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateStarCostVisuals))]
    internal static void MakeStarsInvisible(ref TextureRect ____starIcon)
    {
        if (!MyModConfig.HideStars) return;
        ____starIcon.Visible = false;
    }

    [HarmonyPatch]
    static class HoverTipsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
        internal static void RemoveExtraHoverTooltips(ref IEnumerable<IHoverTip> __result)
        {
            if (MyModConfig.HideTooltips) __result = [];
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), "HoverTips", MethodType.Getter)]
        // [HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
        internal static void RemoveHoverTooltips(CardModel? __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!MyModConfig.HideTooltips) return;
            if (__instance == null) return;
            try
            {
                List<IHoverTip> tips = [];

                if (MyModConfig.ShowCreditsTooltip) tips.AddRange(Credits.Tooltips(__instance));

                if (!MyModConfig.HideTooltips) tips.AddRange(__result);
                __result = tips;
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"HoverTips Error: {e}");
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.ActivateRewardScreenGlow))]
    internal static bool RemoveRewardCardRarityGlow()
    {
        if (MyModConfig.HideCardRewardRarityGlow) return false;
        return true;
    }
    [HarmonyPatch]
    static class DescriptionPatch
    {
        // TODO: make it a class and include hiding the description background
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
        internal static void HideDescription(MegaLabel? ____descriptionLabel)
        {
            if (____descriptionLabel == null) return;
            ____descriptionLabel.Visible = !MyModConfig.HideDescription;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardModel), "GetDescriptionForPile", [typeof(PileType), typeof(DescriptionPreviewType), typeof(Creature)])]
        static internal bool GetDescriptionForPilePatch(ref string __result)
        {
            if (!MyModConfig.HideDescription) return true;
            __result = "";
            return false;
        }
    }


    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
    public class CardTitleIntercept
    {
        [HarmonyPostfix]
        internal static void Intercept(ref CardModel? __instance, ref String __result)
        {
            if (__instance == null) return;
            if (!MyModConfig.HideTitle) return;
            __result = FormattedText(__instance);
        }

        private static string FormattedText(CardModel card)
        {
            if (!card.IsUpgraded) return "";
            if (card.MaxUpgradeLevel <= 1) return "+";
            return $"+{card.CurrentUpgradeLevel}";
        }
    }
}